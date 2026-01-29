using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Testcontainers.MongoDb;
using TraliVali.Api.Controllers;
using TraliVali.Api.Models;
using TraliVali.Domain.Entities;
using TraliVali.Infrastructure.Data;
using TraliVali.Infrastructure.Repositories;

namespace TraliVali.Tests.Controllers;

/// <summary>
/// Integration tests for ConversationsController
/// </summary>
[Collection("Sequential")]
public class ConversationsControllerIntegrationTests : IAsyncLifetime
{
    private MongoDbContainer? _mongoContainer;
    private MongoDbContext? _dbContext;
    private ConversationsController? _controller;
    private IRepository<Conversation>? _conversationRepository;
    private IRepository<User>? _userRepository;
    private User? _testUser1;
    private User? _testUser2;
    private User? _testUser3;

    public async Task InitializeAsync()
    {
        // Start MongoDB container
        _mongoContainer = new MongoDbBuilder()
            .WithImage("mongo:6.0")
            .Build();
        await _mongoContainer.StartAsync();

        // Setup MongoDB
        var mongoConnectionString = _mongoContainer.GetConnectionString();
        _dbContext = new MongoDbContext(mongoConnectionString, "tralivali_test");
        _conversationRepository = new ConversationRepository(_dbContext);
        _userRepository = new UserRepository(_dbContext);

        // Create test users
        _testUser1 = await _userRepository.AddAsync(new User
        {
            Email = "user1@example.com",
            DisplayName = "User One",
            PasswordHash = "hash1",
            PublicKey = "key1",
            IsActive = true
        });

        _testUser2 = await _userRepository.AddAsync(new User
        {
            Email = "user2@example.com",
            DisplayName = "User Two",
            PasswordHash = "hash2",
            PublicKey = "key2",
            IsActive = true
        });

        _testUser3 = await _userRepository.AddAsync(new User
        {
            Email = "user3@example.com",
            DisplayName = "User Three",
            PasswordHash = "hash3",
            PublicKey = "key3",
            IsActive = true
        });

        // Setup controller
        var logger = new Mock<ILogger<ConversationsController>>();
        _controller = new ConversationsController(
            _conversationRepository,
            _userRepository,
            logger.Object
        );
    }

    public async Task DisposeAsync()
    {
        if (_mongoContainer != null)
        {
            await _mongoContainer.StopAsync();
            await _mongoContainer.DisposeAsync();
        }
    }

    private void SetupControllerContext(string userId)
    {
        _controller!.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new System.Security.Claims.ClaimsPrincipal(
                    new System.Security.Claims.ClaimsIdentity(new[]
                    {
                        new System.Security.Claims.Claim("userId", userId)
                    }))
            }
        };
    }

    [Fact]
    public async Task CreateDirectConversation_ShouldCreateConversation_WhenValid()
    {
        // Arrange
        SetupControllerContext(_testUser1!.Id);
        var request = new CreateDirectConversationRequest
        {
            OtherUserId = _testUser2!.Id
        };

        // Act
        var result = await _controller!.CreateDirectConversation(request, CancellationToken.None);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result);
        var response = Assert.IsType<ConversationResponse>(createdResult.Value);
        Assert.NotEmpty(response.Id);
        Assert.Equal("direct", response.Type);
        Assert.False(response.IsGroup);
        Assert.Equal(2, response.Participants.Count);
        Assert.Contains(response.Participants, p => p.UserId == _testUser1.Id);
        Assert.Contains(response.Participants, p => p.UserId == _testUser2.Id);
    }

    [Fact]
    public async Task CreateDirectConversation_ShouldReturnExisting_WhenConversationExists()
    {
        // Arrange
        SetupControllerContext(_testUser1!.Id);
        
        // Create first conversation
        var firstRequest = new CreateDirectConversationRequest
        {
            OtherUserId = _testUser2!.Id
        };
        var firstResult = await _controller!.CreateDirectConversation(firstRequest, CancellationToken.None);
        var firstCreatedResult = Assert.IsType<CreatedAtActionResult>(firstResult);
        var firstResponse = Assert.IsType<ConversationResponse>(firstCreatedResult.Value);

        // Try to create duplicate
        var secondRequest = new CreateDirectConversationRequest
        {
            OtherUserId = _testUser2.Id
        };

        // Act
        var result = await _controller.CreateDirectConversation(secondRequest, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ConversationResponse>(okResult.Value);
        Assert.Equal(firstResponse.Id, response.Id);
    }

    [Fact]
    public async Task CreateDirectConversation_ShouldReturnBadRequest_WhenSelfConversation()
    {
        // Arrange
        SetupControllerContext(_testUser1!.Id);
        var request = new CreateDirectConversationRequest
        {
            OtherUserId = _testUser1.Id
        };

        // Act
        var result = await _controller!.CreateDirectConversation(request, CancellationToken.None);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task CreateDirectConversation_ShouldReturnNotFound_WhenUserDoesNotExist()
    {
        // Arrange
        SetupControllerContext(_testUser1!.Id);
        var request = new CreateDirectConversationRequest
        {
            OtherUserId = "000000000000000000000000"
        };

        // Act
        var result = await _controller!.CreateDirectConversation(request, CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task CreateGroupConversation_ShouldCreateConversation_WhenValid()
    {
        // Arrange
        SetupControllerContext(_testUser1!.Id);
        var request = new CreateGroupConversationRequest
        {
            Name = "Test Group",
            MemberUserIds = new List<string> { _testUser2!.Id, _testUser3!.Id }
        };

        // Act
        var result = await _controller!.CreateGroupConversation(request, CancellationToken.None);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result);
        var response = Assert.IsType<ConversationResponse>(createdResult.Value);
        Assert.NotEmpty(response.Id);
        Assert.Equal("group", response.Type);
        Assert.Equal("Test Group", response.Name);
        Assert.True(response.IsGroup);
        Assert.Equal(3, response.Participants.Count);
        
        // Creator should be admin
        var creator = response.Participants.First(p => p.UserId == _testUser1.Id);
        Assert.Equal("admin", creator.Role);
        
        // Others should be members
        Assert.All(response.Participants.Where(p => p.UserId != _testUser1.Id), 
            p => Assert.Equal("member", p.Role));
    }

    [Fact]
    public async Task CreateGroupConversation_ShouldIncludeCreator_WhenNotInMemberList()
    {
        // Arrange
        SetupControllerContext(_testUser1!.Id);
        var request = new CreateGroupConversationRequest
        {
            Name = "Test Group",
            MemberUserIds = new List<string> { _testUser2!.Id }
        };

        // Act
        var result = await _controller!.CreateGroupConversation(request, CancellationToken.None);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result);
        var response = Assert.IsType<ConversationResponse>(createdResult.Value);
        Assert.Equal(2, response.Participants.Count);
        Assert.Contains(response.Participants, p => p.UserId == _testUser1.Id);
    }

    [Fact]
    public async Task CreateGroupConversation_ShouldReturnBadRequest_WhenMemberNotFound()
    {
        // Arrange
        SetupControllerContext(_testUser1!.Id);
        var request = new CreateGroupConversationRequest
        {
            Name = "Test Group",
            MemberUserIds = new List<string> { "000000000000000000000000" }
        };

        // Act
        var result = await _controller!.CreateGroupConversation(request, CancellationToken.None);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task GetConversations_ShouldReturnUserConversations_WithPagination()
    {
        // Arrange
        SetupControllerContext(_testUser1!.Id);
        
        // Create multiple conversations
        await _conversationRepository!.AddAsync(new Conversation
        {
            Type = "direct",
            Participants = new List<Participant>
            {
                new Participant { UserId = _testUser1.Id },
                new Participant { UserId = _testUser2!.Id }
            },
            CreatedAt = DateTime.UtcNow
        });

        await _conversationRepository.AddAsync(new Conversation
        {
            Type = "group",
            Name = "Group 1",
            IsGroup = true,
            Participants = new List<Participant>
            {
                new Participant { UserId = _testUser1.Id },
                new Participant { UserId = _testUser2.Id },
                new Participant { UserId = _testUser3!.Id }
            },
            CreatedAt = DateTime.UtcNow
        });

        // Act
        var result = await _controller!.GetConversations(1, 10, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<PaginatedConversationsResponse>(okResult.Value);
        Assert.Equal(2, response.TotalCount);
        Assert.Equal(2, response.Conversations.Count);
        Assert.Equal(1, response.Page);
        Assert.Equal(10, response.PageSize);
        Assert.Equal(1, response.TotalPages);
    }

    [Fact]
    public async Task GetConversations_ShouldOnlyReturnUserConversations()
    {
        // Arrange
        SetupControllerContext(_testUser1!.Id);
        
        // Create conversation for user1
        await _conversationRepository!.AddAsync(new Conversation
        {
            Type = "direct",
            Participants = new List<Participant>
            {
                new Participant { UserId = _testUser1.Id },
                new Participant { UserId = _testUser2!.Id }
            },
            CreatedAt = DateTime.UtcNow
        });

        // Create conversation not involving user1
        await _conversationRepository.AddAsync(new Conversation
        {
            Type = "direct",
            Participants = new List<Participant>
            {
                new Participant { UserId = _testUser2.Id },
                new Participant { UserId = _testUser3!.Id }
            },
            CreatedAt = DateTime.UtcNow
        });

        // Act
        var result = await _controller!.GetConversations(1, 10, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<PaginatedConversationsResponse>(okResult.Value);
        Assert.Equal(1, response.TotalCount);
        Assert.Single(response.Conversations);
    }

    [Fact]
    public async Task GetConversation_ShouldReturnConversation_WhenUserIsParticipant()
    {
        // Arrange
        SetupControllerContext(_testUser1!.Id);
        
        var conversation = await _conversationRepository!.AddAsync(new Conversation
        {
            Type = "direct",
            Participants = new List<Participant>
            {
                new Participant { UserId = _testUser1.Id },
                new Participant { UserId = _testUser2!.Id }
            },
            CreatedAt = DateTime.UtcNow
        });

        // Act
        var result = await _controller!.GetConversation(conversation.Id, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ConversationResponse>(okResult.Value);
        Assert.Equal(conversation.Id, response.Id);
    }

    [Fact]
    public async Task GetConversation_ShouldReturnForbidden_WhenUserIsNotParticipant()
    {
        // Arrange
        SetupControllerContext(_testUser1!.Id);
        
        var conversation = await _conversationRepository!.AddAsync(new Conversation
        {
            Type = "direct",
            Participants = new List<Participant>
            {
                new Participant { UserId = _testUser2!.Id },
                new Participant { UserId = _testUser3!.Id }
            },
            CreatedAt = DateTime.UtcNow
        });

        // Act
        var result = await _controller!.GetConversation(conversation.Id, CancellationToken.None);

        // Assert
        var statusResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status403Forbidden, statusResult.StatusCode);
    }

    [Fact]
    public async Task GetConversation_ShouldReturnNotFound_WhenConversationDoesNotExist()
    {
        // Arrange
        SetupControllerContext(_testUser1!.Id);

        // Act
        var result = await _controller!.GetConversation("000000000000000000000000", CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task UpdateGroupMetadata_ShouldUpdateMetadata_WhenUserIsAdmin()
    {
        // Arrange
        SetupControllerContext(_testUser1!.Id);
        
        var conversation = await _conversationRepository!.AddAsync(new Conversation
        {
            Type = "group",
            Name = "Original Name",
            IsGroup = true,
            Participants = new List<Participant>
            {
                new Participant { UserId = _testUser1.Id, Role = "admin" },
                new Participant { UserId = _testUser2!.Id, Role = "member" }
            },
            CreatedAt = DateTime.UtcNow
        });

        var request = new UpdateGroupMetadataRequest
        {
            Name = "Updated Name",
            Metadata = new Dictionary<string, string> { { "key", "value" } }
        };

        // Act
        var result = await _controller!.UpdateGroupMetadata(conversation.Id, request, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ConversationResponse>(okResult.Value);
        Assert.Equal("Updated Name", response.Name);
        Assert.Contains(response.Metadata, kvp => kvp.Key == "key" && kvp.Value == "value");
    }

    [Fact]
    public async Task UpdateGroupMetadata_ShouldReturnForbidden_WhenUserIsNotAdmin()
    {
        // Arrange
        SetupControllerContext(_testUser2!.Id);
        
        var conversation = await _conversationRepository!.AddAsync(new Conversation
        {
            Type = "group",
            Name = "Original Name",
            IsGroup = true,
            Participants = new List<Participant>
            {
                new Participant { UserId = _testUser1!.Id, Role = "admin" },
                new Participant { UserId = _testUser2.Id, Role = "member" }
            },
            CreatedAt = DateTime.UtcNow
        });

        var request = new UpdateGroupMetadataRequest
        {
            Name = "Updated Name"
        };

        // Act
        var result = await _controller!.UpdateGroupMetadata(conversation.Id, request, CancellationToken.None);

        // Assert
        var statusResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status403Forbidden, statusResult.StatusCode);
    }

    [Fact]
    public async Task UpdateGroupMetadata_ShouldReturnBadRequest_WhenNotGroupConversation()
    {
        // Arrange
        SetupControllerContext(_testUser1!.Id);
        
        var conversation = await _conversationRepository!.AddAsync(new Conversation
        {
            Type = "direct",
            IsGroup = false,
            Participants = new List<Participant>
            {
                new Participant { UserId = _testUser1.Id },
                new Participant { UserId = _testUser2!.Id }
            },
            CreatedAt = DateTime.UtcNow
        });

        var request = new UpdateGroupMetadataRequest
        {
            Name = "Updated Name"
        };

        // Act
        var result = await _controller!.UpdateGroupMetadata(conversation.Id, request, CancellationToken.None);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task AddMember_ShouldAddMember_WhenUserIsAdmin()
    {
        // Arrange
        SetupControllerContext(_testUser1!.Id);
        
        var conversation = await _conversationRepository!.AddAsync(new Conversation
        {
            Type = "group",
            Name = "Test Group",
            IsGroup = true,
            Participants = new List<Participant>
            {
                new Participant { UserId = _testUser1.Id, Role = "admin" },
                new Participant { UserId = _testUser2!.Id, Role = "member" }
            },
            CreatedAt = DateTime.UtcNow
        });

        var request = new AddMemberRequest
        {
            UserId = _testUser3!.Id,
            Role = "member"
        };

        // Act
        var result = await _controller!.AddMember(conversation.Id, request, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ConversationResponse>(okResult.Value);
        Assert.Equal(3, response.Participants.Count);
        Assert.Contains(response.Participants, p => p.UserId == _testUser3.Id);
    }

    [Fact]
    public async Task AddMember_ShouldReturnForbidden_WhenUserIsNotAdmin()
    {
        // Arrange
        SetupControllerContext(_testUser2!.Id);
        
        var conversation = await _conversationRepository!.AddAsync(new Conversation
        {
            Type = "group",
            Name = "Test Group",
            IsGroup = true,
            Participants = new List<Participant>
            {
                new Participant { UserId = _testUser1!.Id, Role = "admin" },
                new Participant { UserId = _testUser2.Id, Role = "member" }
            },
            CreatedAt = DateTime.UtcNow
        });

        var request = new AddMemberRequest
        {
            UserId = _testUser3!.Id
        };

        // Act
        var result = await _controller!.AddMember(conversation.Id, request, CancellationToken.None);

        // Assert
        var statusResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status403Forbidden, statusResult.StatusCode);
    }

    [Fact]
    public async Task AddMember_ShouldReturnBadRequest_WhenUserAlreadyMember()
    {
        // Arrange
        SetupControllerContext(_testUser1!.Id);
        
        var conversation = await _conversationRepository!.AddAsync(new Conversation
        {
            Type = "group",
            Name = "Test Group",
            IsGroup = true,
            Participants = new List<Participant>
            {
                new Participant { UserId = _testUser1.Id, Role = "admin" },
                new Participant { UserId = _testUser2!.Id, Role = "member" }
            },
            CreatedAt = DateTime.UtcNow
        });

        var request = new AddMemberRequest
        {
            UserId = _testUser2.Id
        };

        // Act
        var result = await _controller!.AddMember(conversation.Id, request, CancellationToken.None);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task AddMember_ShouldReturnBadRequest_WhenNotGroupConversation()
    {
        // Arrange
        SetupControllerContext(_testUser1!.Id);
        
        var conversation = await _conversationRepository!.AddAsync(new Conversation
        {
            Type = "direct",
            IsGroup = false,
            Participants = new List<Participant>
            {
                new Participant { UserId = _testUser1.Id },
                new Participant { UserId = _testUser2!.Id }
            },
            CreatedAt = DateTime.UtcNow
        });

        var request = new AddMemberRequest
        {
            UserId = _testUser3!.Id
        };

        // Act
        var result = await _controller!.AddMember(conversation.Id, request, CancellationToken.None);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task AddMember_ShouldReturnNotFound_WhenUserToAddDoesNotExist()
    {
        // Arrange
        SetupControllerContext(_testUser1!.Id);
        
        var conversation = await _conversationRepository!.AddAsync(new Conversation
        {
            Type = "group",
            Name = "Test Group",
            IsGroup = true,
            Participants = new List<Participant>
            {
                new Participant { UserId = _testUser1.Id, Role = "admin" },
                new Participant { UserId = _testUser2!.Id, Role = "member" }
            },
            CreatedAt = DateTime.UtcNow
        });

        var request = new AddMemberRequest
        {
            UserId = "000000000000000000000000"
        };

        // Act
        var result = await _controller!.AddMember(conversation.Id, request, CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task AddMember_ShouldReturnBadRequest_WhenTryingToAddAdmin()
    {
        // Arrange
        SetupControllerContext(_testUser1!.Id);
        
        var conversation = await _conversationRepository!.AddAsync(new Conversation
        {
            Type = "group",
            Name = "Test Group",
            IsGroup = true,
            Participants = new List<Participant>
            {
                new Participant { UserId = _testUser1.Id, Role = "admin" },
                new Participant { UserId = _testUser2!.Id, Role = "member" }
            },
            CreatedAt = DateTime.UtcNow
        });

        var request = new AddMemberRequest
        {
            UserId = _testUser3!.Id,
            Role = "admin"
        };

        // Act
        var result = await _controller!.AddMember(conversation.Id, request, CancellationToken.None);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task RemoveMember_ShouldRemoveMember_WhenUserIsAdmin()
    {
        // Arrange
        SetupControllerContext(_testUser1!.Id);
        
        var conversation = await _conversationRepository!.AddAsync(new Conversation
        {
            Type = "group",
            Name = "Test Group",
            IsGroup = true,
            Participants = new List<Participant>
            {
                new Participant { UserId = _testUser1.Id, Role = "admin" },
                new Participant { UserId = _testUser2!.Id, Role = "member" },
                new Participant { UserId = _testUser3!.Id, Role = "member" }
            },
            CreatedAt = DateTime.UtcNow
        });

        // Act
        var result = await _controller!.RemoveMember(conversation.Id, _testUser3.Id, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ConversationResponse>(okResult.Value);
        Assert.Equal(2, response.Participants.Count);
        Assert.DoesNotContain(response.Participants, p => p.UserId == _testUser3.Id);
    }

    [Fact]
    public async Task RemoveMember_ShouldAllowSelfRemoval()
    {
        // Arrange
        SetupControllerContext(_testUser2!.Id);
        
        var conversation = await _conversationRepository!.AddAsync(new Conversation
        {
            Type = "group",
            Name = "Test Group",
            IsGroup = true,
            Participants = new List<Participant>
            {
                new Participant { UserId = _testUser1!.Id, Role = "admin" },
                new Participant { UserId = _testUser2.Id, Role = "member" },
                new Participant { UserId = _testUser3!.Id, Role = "member" }
            },
            CreatedAt = DateTime.UtcNow
        });

        // Act
        var result = await _controller!.RemoveMember(conversation.Id, _testUser2.Id, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ConversationResponse>(okResult.Value);
        Assert.Equal(2, response.Participants.Count);
        Assert.DoesNotContain(response.Participants, p => p.UserId == _testUser2.Id);
    }

    [Fact]
    public async Task RemoveMember_ShouldReturnForbidden_WhenNonAdminRemovingOther()
    {
        // Arrange
        SetupControllerContext(_testUser2!.Id);
        
        var conversation = await _conversationRepository!.AddAsync(new Conversation
        {
            Type = "group",
            Name = "Test Group",
            IsGroup = true,
            Participants = new List<Participant>
            {
                new Participant { UserId = _testUser1!.Id, Role = "admin" },
                new Participant { UserId = _testUser2.Id, Role = "member" },
                new Participant { UserId = _testUser3!.Id, Role = "member" }
            },
            CreatedAt = DateTime.UtcNow
        });

        // Act - User2 trying to remove User3
        var result = await _controller!.RemoveMember(conversation.Id, _testUser3.Id, CancellationToken.None);

        // Assert
        var statusResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status403Forbidden, statusResult.StatusCode);
    }

    [Fact]
    public async Task RemoveMember_ShouldReturnNotFound_WhenUserNotInConversation()
    {
        // Arrange
        SetupControllerContext(_testUser1!.Id);
        
        var conversation = await _conversationRepository!.AddAsync(new Conversation
        {
            Type = "group",
            Name = "Test Group",
            IsGroup = true,
            Participants = new List<Participant>
            {
                new Participant { UserId = _testUser1.Id, Role = "admin" },
                new Participant { UserId = _testUser2!.Id, Role = "member" }
            },
            CreatedAt = DateTime.UtcNow
        });

        // Act
        var result = await _controller!.RemoveMember(conversation.Id, _testUser3!.Id, CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task RemoveMember_ShouldReturnBadRequest_WhenNotGroupConversation()
    {
        // Arrange
        SetupControllerContext(_testUser1!.Id);
        
        var conversation = await _conversationRepository!.AddAsync(new Conversation
        {
            Type = "direct",
            IsGroup = false,
            Participants = new List<Participant>
            {
                new Participant { UserId = _testUser1.Id },
                new Participant { UserId = _testUser2!.Id }
            },
            CreatedAt = DateTime.UtcNow
        });

        // Act
        var result = await _controller!.RemoveMember(conversation.Id, _testUser2.Id, CancellationToken.None);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }
}
