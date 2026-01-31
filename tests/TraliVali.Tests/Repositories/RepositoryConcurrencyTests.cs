using System.Collections.Concurrent;
using TraliVali.Domain.Entities;
using TraliVali.Infrastructure.Repositories;
using TraliVali.Tests.Infrastructure;

namespace TraliVali.Tests.Repositories;

/// <summary>
/// Tests for repository concurrent operations
/// </summary>
public class RepositoryConcurrencyTests : IClassFixture<MongoDbFixture>
{
    private readonly MongoDbFixture _fixture;

    public RepositoryConcurrencyTests(MongoDbFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task ConcurrentAdd_ShouldAddAllUsers()
    {
        // Arrange
        await _fixture.CleanupAsync();
        var repository = new UserRepository(_fixture.Context);
        var userCount = 50;
        var tasks = new List<Task<User>>();

        // Act
        for (int i = 0; i < userCount; i++)
        {
            var user = new User
            {
                Email = $"user{i}@example.com",
                DisplayName = $"User {i}",
                IsActive = true
            };
            tasks.Add(repository.AddAsync(user));
        }

        var results = await Task.WhenAll(tasks);

        // Assert
        Assert.Equal(userCount, results.Length);
        Assert.All(results, user => Assert.NotEmpty(user.Id));

        var allUsers = await repository.GetAllAsync();
        Assert.Equal(userCount, allUsers.Count());
    }

    [Fact]
    public async Task ConcurrentRead_ShouldReturnSameUser()
    {
        // Arrange
        await _fixture.CleanupAsync();
        var repository = new UserRepository(_fixture.Context);
        var user = new User
        {
            Email = "test@example.com",
            DisplayName = "Test User"
        };
        var addedUser = await repository.AddAsync(user);
        var readCount = 100;
        var tasks = new List<Task<User?>>();

        // Act
        for (int i = 0; i < readCount; i++)
        {
            tasks.Add(repository.GetByIdAsync(addedUser.Id));
        }

        var results = await Task.WhenAll(tasks);

        // Assert
        Assert.Equal(readCount, results.Length);
        Assert.All(results, u => 
        {
            Assert.NotNull(u);
            Assert.Equal(addedUser.Id, u.Id);
            Assert.Equal("test@example.com", u.Email);
        });
    }

    [Fact]
    public async Task ConcurrentUpdate_ShouldHandleRaceCondition()
    {
        // Arrange
        await _fixture.CleanupAsync();
        var repository = new UserRepository(_fixture.Context);
        var user = new User
        {
            Email = "test@example.com",
            DisplayName = "Original Name"
        };
        var addedUser = await repository.AddAsync(user);
        var updateCount = 20;
        var tasks = new List<Task<bool>>();

        // Act - Multiple concurrent updates
        for (int i = 0; i < updateCount; i++)
        {
            var index = i;
            tasks.Add(Task.Run(async () =>
            {
                var userToUpdate = await repository.GetByIdAsync(addedUser.Id);
                if (userToUpdate != null)
                {
                    userToUpdate.DisplayName = $"Updated Name {index}";
                    return await repository.UpdateAsync(userToUpdate.Id, userToUpdate);
                }
                return false;
            }));
        }

        var results = await Task.WhenAll(tasks);

        // Assert - At least one update should succeed
        Assert.Contains(true, results);

        // Verify the final state
        var finalUser = await repository.GetByIdAsync(addedUser.Id);
        Assert.NotNull(finalUser);
        Assert.StartsWith("Updated Name", finalUser.DisplayName);
    }

    [Fact]
    public async Task ConcurrentDelete_ShouldHandleMultipleDeletes()
    {
        // Arrange
        await _fixture.CleanupAsync();
        var repository = new UserRepository(_fixture.Context);
        var user = new User
        {
            Email = "test@example.com",
            DisplayName = "Test User"
        };
        var addedUser = await repository.AddAsync(user);
        var deleteCount = 10;
        var tasks = new List<Task<bool>>();

        // Act - Multiple concurrent delete attempts
        for (int i = 0; i < deleteCount; i++)
        {
            tasks.Add(repository.DeleteAsync(addedUser.Id));
        }

        var results = await Task.WhenAll(tasks);

        // Assert - Only one delete should succeed
        Assert.Single(results.Where(r => r == true));
        Assert.Equal(deleteCount - 1, results.Count(r => r == false));

        // Verify the user is deleted
        var deletedUser = await repository.GetByIdAsync(addedUser.Id);
        Assert.Null(deletedUser);
    }

    [Fact]
    public async Task ConcurrentFind_ShouldReturnConsistentResults()
    {
        // Arrange
        await _fixture.CleanupAsync();
        var repository = new UserRepository(_fixture.Context);
        
        // Add active and inactive users
        for (int i = 0; i < 10; i++)
        {
            await repository.AddAsync(new User
            {
                Email = $"active{i}@example.com",
                DisplayName = $"Active User {i}",
                IsActive = true
            });
        }
        for (int i = 0; i < 5; i++)
        {
            await repository.AddAsync(new User
            {
                Email = $"inactive{i}@example.com",
                DisplayName = $"Inactive User {i}",
                IsActive = false
            });
        }

        var findCount = 50;
        var tasks = new List<Task<IEnumerable<User>>>();

        // Act
        for (int i = 0; i < findCount; i++)
        {
            tasks.Add(repository.FindAsync(u => u.IsActive));
        }

        var results = await Task.WhenAll(tasks);

        // Assert
        Assert.All(results, result =>
        {
            Assert.Equal(10, result.Count());
            Assert.All(result, u => Assert.True(u.IsActive));
        });
    }

    [Fact]
    public async Task ConcurrentCount_ShouldReturnConsistentCount()
    {
        // Arrange
        await _fixture.CleanupAsync();
        var repository = new UserRepository(_fixture.Context);
        
        // Add users
        for (int i = 0; i < 25; i++)
        {
            await repository.AddAsync(new User
            {
                Email = $"user{i}@example.com",
                DisplayName = $"User {i}",
                IsActive = i % 2 == 0
            });
        }

        var countCount = 50;
        var tasks = new List<Task<long>>();

        // Act
        for (int i = 0; i < countCount; i++)
        {
            tasks.Add(repository.CountAsync());
        }

        var results = await Task.WhenAll(tasks);

        // Assert
        Assert.All(results, count => Assert.Equal(25, count));
    }

    [Fact]
    public async Task ConcurrentOperationsOnDifferentRepositories_ShouldSucceed()
    {
        // Arrange
        await _fixture.CleanupAsync();
        var userRepository = new UserRepository(_fixture.Context);
        var messageRepository = new MessageRepository(_fixture.Context);
        var conversationRepository = new ConversationRepository(_fixture.Context);
        
        var operationCount = 30;
        var tasks = new List<Task>();

        // Act - Mix operations across different repositories
        for (int i = 0; i < operationCount; i++)
        {
            var index = i;
            tasks.Add(userRepository.AddAsync(new User
            {
                Email = $"user{index}@example.com",
                DisplayName = $"User {index}"
            }));

            tasks.Add(messageRepository.AddAsync(new Message
            {
                ConversationId = "507f1f77bcf86cd799439011",
                SenderId = "507f1f77bcf86cd799439012",
                Content = $"Message {index}"
            }));

            tasks.Add(conversationRepository.AddAsync(new Conversation
            {
                Name = $"Conversation {index}",
                Participants = new List<Participant>()
            }));
        }

        await Task.WhenAll(tasks);

        // Assert
        var userCount = await userRepository.CountAsync();
        var messageCount = await messageRepository.CountAsync();
        var conversationCount = await conversationRepository.CountAsync();

        Assert.Equal(operationCount, userCount);
        Assert.Equal(operationCount, messageCount);
        Assert.Equal(operationCount, conversationCount);
    }

    [Fact]
    public async Task ConcurrentAddWithUniqueConstraint_ShouldHandleDuplicates()
    {
        // Arrange
        await _fixture.CleanupAsync();
        var repository = new UserRepository(_fixture.Context);
        var duplicateEmail = "duplicate@example.com";
        var attemptCount = 10;
        var successCount = new ConcurrentBag<bool>();
        var exceptionCount = new ConcurrentBag<bool>();

        // Act - Try to add users with same email concurrently
        var tasks = Enumerable.Range(0, attemptCount).Select(async i =>
        {
            try
            {
                await _fixture.Context.Users.InsertOneAsync(new User
                {
                    Email = duplicateEmail,
                    DisplayName = $"User {i}"
                });
                successCount.Add(true);
            }
            catch (MongoDB.Driver.MongoWriteException)
            {
                exceptionCount.Add(true);
            }
        });

        await Task.WhenAll(tasks);

        // Assert - Only one should succeed, rest should fail
        Assert.Equal(1, successCount.Count);
        Assert.Equal(attemptCount - 1, exceptionCount.Count);

        var users = await repository.FindAsync(u => u.Email == duplicateEmail);
        Assert.Single(users);
    }

    [Fact]
    public async Task ConcurrentMixedOperations_ShouldMaintainDataIntegrity()
    {
        // Arrange
        await _fixture.CleanupAsync();
        var repository = new UserRepository(_fixture.Context);
        var initialUserCount = 20;
        var userIds = new ConcurrentBag<string>();

        // Add initial users
        for (int i = 0; i < initialUserCount; i++)
        {
            var user = await repository.AddAsync(new User
            {
                Email = $"user{i}@example.com",
                DisplayName = $"User {i}",
                IsActive = true
            });
            userIds.Add(user.Id);
        }

        var tasks = new List<Task>();

        // Act - Mix of concurrent operations
        // Add new users
        for (int i = initialUserCount; i < initialUserCount + 10; i++)
        {
            var index = i;
            tasks.Add(Task.Run(async () =>
            {
                var user = await repository.AddAsync(new User
                {
                    Email = $"user{index}@example.com",
                    DisplayName = $"User {index}",
                    IsActive = true
                });
                userIds.Add(user.Id);
            }));
        }

        // Update existing users
        foreach (var userId in userIds.Take(5))
        {
            var id = userId;
            tasks.Add(Task.Run(async () =>
            {
                var user = await repository.GetByIdAsync(id);
                if (user != null)
                {
                    user.DisplayName = $"Updated {user.DisplayName}";
                    await repository.UpdateAsync(user.Id, user);
                }
            }));
        }

        // Read users
        for (int i = 0; i < 20; i++)
        {
            tasks.Add(repository.GetAllAsync());
        }

        // Delete some users
        foreach (var userId in userIds.Skip(10).Take(5))
        {
            var id = userId;
            tasks.Add(repository.DeleteAsync(id));
        }

        await Task.WhenAll(tasks);

        // Assert - Verify final state
        var finalCount = await repository.CountAsync();
        Assert.Equal(25, finalCount); // 20 initial + 10 added - 5 deleted
    }

    [Fact]
    public async Task ConcurrentTransactionalOperations_ShouldMaintainConsistency()
    {
        // Arrange
        await _fixture.CleanupAsync();
        var userRepository = new UserRepository(_fixture.Context);
        var inviteRepository = new InviteRepository(_fixture.Context);
        
        var operationCount = 20;
        var tasks = new List<Task>();

        // Act - Simulate concurrent user registration with invites
        for (int i = 0; i < operationCount; i++)
        {
            var index = i;
            tasks.Add(Task.Run(async () =>
            {
                // Add user
                var user = await userRepository.AddAsync(new User
                {
                    Email = $"user{index}@example.com",
                    DisplayName = $"User {index}"
                });

                // Add corresponding invite
                await inviteRepository.AddAsync(new Invite
                {
                    Token = Guid.NewGuid().ToString(),
                    Email = user.Email,
                    InviterId = "507f1f77bcf86cd799439011",
                    ExpiresAt = DateTime.UtcNow.AddDays(7),
                    IsUsed = true,
                    UsedBy = user.Id,
                    UsedAt = DateTime.UtcNow
                });
            }));
        }

        await Task.WhenAll(tasks);

        // Assert
        var userCount = await userRepository.CountAsync();
        var inviteCount = await inviteRepository.CountAsync();
        
        Assert.Equal(operationCount, userCount);
        Assert.Equal(operationCount, inviteCount);

        // Verify all invites are marked as used
        var usedInvites = await inviteRepository.FindAsync(i => i.IsUsed);
        Assert.Equal(operationCount, usedInvites.Count());
    }
}
