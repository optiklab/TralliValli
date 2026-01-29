using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Testcontainers.MongoDb;
using TraliVali.Api.Controllers;
using TraliVali.Api.Models;
using TraliVali.Auth;
using TraliVali.Domain.Entities;
using TraliVali.Infrastructure.Data;
using TraliVali.Infrastructure.Storage;

namespace TraliVali.Tests.Controllers;

/// <summary>
/// Integration tests for AdminController
/// </summary>
[Collection("Sequential")]
public class AdminControllerIntegrationTests : IAsyncLifetime
{
    private MongoDbContainer? _mongoContainer;
    private MongoDbContext? _dbContext;
    private AdminController? _controller;
    private Mock<IBackupService>? _mockBackupService;
    private Mock<IAzureBlobService>? _mockBlobService;

    public async Task InitializeAsync()
    {
        // Start MongoDB container
        _mongoContainer = new MongoDbBuilder().Build();
        await _mongoContainer.StartAsync();

        // Setup MongoDB
        var mongoConnectionString = _mongoContainer.GetConnectionString();
        _dbContext = new MongoDbContext(mongoConnectionString, "tralivali_test");

        // Setup mock services
        _mockBackupService = new Mock<IBackupService>();
        _mockBlobService = new Mock<IAzureBlobService>();

        // Setup controller
        var logger = new Mock<ILogger<AdminController>>();
        _controller = new AdminController(
            _dbContext.Messages,
            _dbContext.ArchivalStats,
            _mockBackupService.Object,
            _mockBlobService.Object,
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

    private void SetupAdminUser()
    {
        var claims = new List<Claim>
        {
            new Claim("userId", "admin123"),
            new Claim("email", "admin@example.com"),
            new Claim("displayName", "Admin User"),
            new Claim("role", "admin")
        };
        
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        
        _controller!.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = claimsPrincipal
            }
        };
    }

    private void SetupNonAdminUser()
    {
        var claims = new List<Claim>
        {
            new Claim("userId", "user123"),
            new Claim("email", "user@example.com"),
            new Claim("displayName", "Regular User"),
            new Claim("role", "user")
        };
        
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        
        _controller!.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = claimsPrincipal
            }
        };
    }

    [Fact]
    public async Task TriggerArchival_ShouldReturnForbidden_WhenUserIsNotAdmin()
    {
        // Arrange
        SetupNonAdminUser();

        // Act
        var result = await _controller!.TriggerArchival(CancellationToken.None);

        // Assert
        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task TriggerArchival_ShouldReturnSuccess_WhenUserIsAdmin()
    {
        // Arrange
        SetupAdminUser();

        // Act
        var result = await _controller!.TriggerArchival(CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<TriggerArchivalResponse>(okResult.Value);
        Assert.Equal("Archival completed successfully", response.Message);
        Assert.True(response.MessagesArchived >= 0);
    }

    [Fact]
    public async Task GetArchivalStats_ShouldReturnForbidden_WhenUserIsNotAdmin()
    {
        // Arrange
        SetupNonAdminUser();

        // Act
        var result = await _controller!.GetArchivalStats(CancellationToken.None);

        // Assert
        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task GetArchivalStats_ShouldReturnStats_WhenUserIsAdmin()
    {
        // Arrange
        SetupAdminUser();

        // Insert some test archival stats
        var stats = new ArchivalStats
        {
            RunAt = DateTime.UtcNow,
            MessagesArchived = 100,
            StorageUsed = 50000,
            Status = "Success"
        };
        await _dbContext!.ArchivalStats.InsertOneAsync(stats);

        // Act
        var result = await _controller!.GetArchivalStats(CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ArchivalStatsResponse>(okResult.Value);
        Assert.NotNull(response.LastRunAt);
        Assert.Equal(100, response.TotalMessagesArchived);
        Assert.Equal(50000, response.TotalStorageUsed);
        Assert.Equal("Success", response.LastRunStatus);
    }

    [Fact]
    public async Task TriggerBackup_ShouldReturnForbidden_WhenUserIsNotAdmin()
    {
        // Arrange
        SetupNonAdminUser();

        // Act
        var result = await _controller!.TriggerBackup(CancellationToken.None);

        // Assert
        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task TriggerBackup_ShouldReturnSuccess_WhenUserIsAdmin()
    {
        // Arrange
        SetupAdminUser();

        var backup = new Backup
        {
            Id = "backup123",
            CreatedAt = DateTime.UtcNow,
            FilePath = "backups/2024-01-01",
            Size = 1000000,
            Type = "manual",
            Status = BackupStatus.Completed
        };

        _mockBackupService!
            .Setup(x => x.TriggerBackupAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(backup);

        // Act
        var result = await _controller!.TriggerBackup(CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<TriggerBackupResponse>(okResult.Value);
        Assert.Equal("backup123", response.BackupId);
        Assert.Equal("Backup completed successfully", response.Message);
        Assert.Equal("Completed", response.Status);

        _mockBackupService.Verify(
            x => x.TriggerBackupAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ListBackups_ShouldReturnForbidden_WhenUserIsNotAdmin()
    {
        // Arrange
        SetupNonAdminUser();

        // Act
        var result = await _controller!.ListBackups(CancellationToken.None);

        // Assert
        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task ListBackups_ShouldReturnBackupList_WhenUserIsAdmin()
    {
        // Arrange
        SetupAdminUser();

        var backups = new List<Backup>
        {
            new Backup
            {
                Id = "backup1",
                CreatedAt = DateTime.UtcNow.AddDays(-1),
                FilePath = "backups/2024-01-01",
                Size = 1000000,
                Type = "manual",
                Status = BackupStatus.Completed
            },
            new Backup
            {
                Id = "backup2",
                CreatedAt = DateTime.UtcNow,
                FilePath = "backups/2024-01-02",
                Size = 2000000,
                Type = "scheduled",
                Status = BackupStatus.Completed
            }
        };

        _mockBackupService!
            .Setup(x => x.ListBackupsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(backups);

        // Act
        var result = await _controller!.ListBackups(CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<BackupListResponse>(okResult.Value);
        Assert.Equal(2, response.Backups.Count);
        Assert.Contains(response.Backups, b => b.Id == "backup1");
        Assert.Contains(response.Backups, b => b.Id == "backup2");

        _mockBackupService.Verify(
            x => x.ListBackupsAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task RestoreBackup_ShouldReturnForbidden_WhenUserIsNotAdmin()
    {
        // Arrange
        SetupNonAdminUser();

        // Act
        var result = await _controller!.RestoreBackup("2024-01-01", CancellationToken.None);

        // Assert
        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task RestoreBackup_ShouldReturnSuccess_WhenUserIsAdmin()
    {
        // Arrange
        SetupAdminUser();

        _mockBackupService!
            .Setup(x => x.RestoreBackupAsync("2024-01-01", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _controller!.RestoreBackup("2024-01-01", CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<RestoreBackupResponse>(okResult.Value);
        Assert.True(response.Success);
        Assert.Equal("Backup restored successfully", response.Message);

        _mockBackupService.Verify(
            x => x.RestoreBackupAsync("2024-01-01", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task RestoreBackup_ShouldReturnBadRequest_WhenDateIsEmpty()
    {
        // Arrange
        SetupAdminUser();

        // Act
        var result = await _controller!.RestoreBackup("", CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.NotNull(badRequestResult.Value);
    }

    [Fact]
    public async Task ListArchives_ShouldReturnForbidden_WhenUserIsNotAdmin()
    {
        // Arrange
        SetupNonAdminUser();

        // Act
        var result = await _controller!.ListArchives(CancellationToken.None);

        // Assert
        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task ListArchives_ShouldReturnArchiveList_WhenUserIsAdmin()
    {
        // Arrange
        SetupAdminUser();

        var archivePaths = new List<string>
        {
            "archives/2024/01/messages_conv1_2024-01-01.json",
            "archives/2024/01/messages_conv2_2024-01-02.json"
        };

        _mockBlobService!
            .Setup(x => x.ListArchivesAsync("archives/", It.IsAny<CancellationToken>()))
            .ReturnsAsync(archivePaths);

        // Act
        var result = await _controller!.ListArchives(CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ArchiveListResponse>(okResult.Value);
        Assert.Equal(2, response.Archives.Count);
        Assert.All(response.Archives, archive =>
        {
            Assert.NotEmpty(archive.Path);
            Assert.NotEmpty(archive.DownloadUrl);
        });

        _mockBlobService.Verify(
            x => x.ListArchivesAsync("archives/", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ListArchives_ShouldReturnEmptyList_WhenBlobServiceIsNotConfigured()
    {
        // Arrange
        SetupAdminUser();

        // Create controller with null blob service
        var logger = new Mock<ILogger<AdminController>>();
        var controller = new AdminController(
            _dbContext!.Messages,
            _dbContext.ArchivalStats,
            _mockBackupService!.Object,
            null, // No blob service
            logger.Object
        );

        var claims = new List<Claim>
        {
            new Claim("userId", "admin123"),
            new Claim("role", "admin")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
        };

        // Act
        var result = await controller.ListArchives(CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ArchiveListResponse>(okResult.Value);
        Assert.Empty(response.Archives);
    }
}
