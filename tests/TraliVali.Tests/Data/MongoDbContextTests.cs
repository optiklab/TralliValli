using TraliVali.Infrastructure.Data;

namespace TraliVali.Tests.Data;

/// <summary>
/// Tests for MongoDbContext
/// </summary>
public class MongoDbContextTests
{
    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenConnectionStringIsNull()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new MongoDbContext(null!, "testdb"));
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenConnectionStringIsEmpty()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new MongoDbContext("", "testdb"));
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenConnectionStringIsWhitespace()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new MongoDbContext("   ", "testdb"));
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenDatabaseNameIsNull()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new MongoDbContext("mongodb://localhost:27017", null!));
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenDatabaseNameIsEmpty()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new MongoDbContext("mongodb://localhost:27017", ""));
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenDatabaseNameIsWhitespace()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new MongoDbContext("mongodb://localhost:27017", "   "));
    }
}
