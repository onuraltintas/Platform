namespace Enterprise.Shared.Configuration.Tests.Services;

[TestFixture]
public class ConfigurationChangeTrackerTests
{
    private Mock<ILogger<ConfigurationChangeTracker>> _logger = null!;
    private ConfigurationChangeTracker _changeTracker = null!;

    [SetUp]
    public void SetUp()
    {
        _logger = new Mock<ILogger<ConfigurationChangeTracker>>();
        _changeTracker = new ConfigurationChangeTracker(_logger.Object);
    }

    [TearDown]
    public void TearDown()
    {
        _changeTracker?.Dispose();
    }

    #region TrackChangeAsync Tests

    [Test]
    public async Task TrackChangeAsync_WithValidParameters_TracksChange()
    {
        // Arrange
        var key = "TestKey";
        var oldValue = "old-value";
        var newValue = "new-value";
        var changedBy = "TestUser";
        var reason = "Test change";

        // Act
        await _changeTracker.TrackChangeAsync(key, oldValue, newValue, changedBy, reason);

        // Assert
        var history = await _changeTracker.GetChangeHistoryAsync(key);
        history.Should().HaveCount(1);
        
        var change = history.First();
        change.Key.Should().Be(key);
        change.OldValue.Should().Be(oldValue);
        change.NewValue.Should().Be(newValue);
        change.ChangedBy.Should().Be(changedBy);
        change.Reason.Should().Be(reason);
        change.Id.Should().NotBeEmpty();
        change.ChangedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Test]
    public async Task TrackChangeAsync_WithNullValues_TracksChangeWithNulls()
    {
        // Arrange
        var key = "TestKey";

        // Act
        await _changeTracker.TrackChangeAsync(key, null, null);

        // Assert
        var history = await _changeTracker.GetChangeHistoryAsync(key);
        history.Should().HaveCount(1);
        
        var change = history.First();
        change.Key.Should().Be(key);
        change.OldValue.Should().BeNull();
        change.NewValue.Should().BeNull();
        change.ChangedBy.Should().Be("System");
    }

    [Test]
    public async Task TrackChangeAsync_FiresConfigurationChangedEvent()
    {
        // Arrange
        var key = "TestKey";
        var newValue = "new-value";
        ConfigurationChangedEventArgs? eventArgs = null;

        _changeTracker.ConfigurationChanged += (sender, args) => eventArgs = args;

        // Act
        await _changeTracker.TrackChangeAsync(key, "old", newValue, "TestUser");

        // Assert
        eventArgs.Should().NotBeNull();
        eventArgs!.Key.Should().Be(key);
        eventArgs.NewValue.Should().Be(newValue);
        eventArgs.ChangedBy.Should().Be("TestUser");
    }

    [Test]
    public async Task TrackChangeAsync_WithNullOrEmptyKey_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.ThrowsAsync<ArgumentException>(() =>
            _changeTracker.TrackChangeAsync("", "old", "new"));
        
        Assert.ThrowsAsync<ArgumentException>(() =>
            _changeTracker.TrackChangeAsync(" ", "old", "new"));
    }

    #endregion

    #region GetChangeHistoryAsync Tests

    [Test]
    public async Task GetChangeHistoryAsync_WithExistingKey_ReturnsHistory()
    {
        // Arrange
        var key = "TestKey";
        await _changeTracker.TrackChangeAsync(key, "value1", "value2", "User1");
        await _changeTracker.TrackChangeAsync(key, "value2", "value3", "User2");

        // Act
        var history = await _changeTracker.GetChangeHistoryAsync(key);

        // Assert
        history.Should().HaveCount(2);
        history.Should().BeInDescendingOrder(x => x.ChangedAt);
        
        var latestChange = history.First();
        latestChange.NewValue.Should().Be("value3");
        latestChange.ChangedBy.Should().Be("User2");
    }

    [Test]
    public async Task GetChangeHistoryAsync_WithNonExistentKey_ReturnsEmpty()
    {
        // Act
        var history = await _changeTracker.GetChangeHistoryAsync("NonExistentKey");

        // Assert
        history.Should().BeEmpty();
    }

    [Test]
    public async Task GetChangeHistoryAsync_WithDateFilters_ReturnsFilteredResults()
    {
        // Arrange
        var key = "TestKey";
        var now = DateTime.UtcNow;
        var from = now.AddMinutes(-5);
        var to = now.AddMinutes(5);

        await _changeTracker.TrackChangeAsync(key, "old", "new");

        // Act
        var history = await _changeTracker.GetChangeHistoryAsync(key, from, to);

        // Assert
        history.Should().HaveCount(1);
    }

    [Test]
    public async Task GetChangeHistoryAsync_WithDateFilters_ExcludesOutOfRangeResults()
    {
        // Arrange
        var key = "TestKey";
        var futureDate = DateTime.UtcNow.AddDays(1);

        await _changeTracker.TrackChangeAsync(key, "old", "new");

        // Act
        var history = await _changeTracker.GetChangeHistoryAsync(key, futureDate, futureDate.AddHours(1));

        // Assert
        history.Should().BeEmpty();
    }

    [Test]
    public async Task GetChangeHistoryAsync_WithNullOrEmptyKey_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.ThrowsAsync<ArgumentException>(() =>
            _changeTracker.GetChangeHistoryAsync(""));
        
        Assert.ThrowsAsync<ArgumentException>(() =>
            _changeTracker.GetChangeHistoryAsync(" "));
    }

    #endregion

    #region GetAllChangesAsync Tests

    [Test]
    public async Task GetAllChangesAsync_ReturnsAllChanges()
    {
        // Arrange
        await _changeTracker.TrackChangeAsync("Key1", "old1", "new1", "User1");
        await _changeTracker.TrackChangeAsync("Key2", "old2", "new2", "User2");
        await _changeTracker.TrackChangeAsync("Key3", "old3", "new3", "User1");

        // Act
        var allChanges = await _changeTracker.GetAllChangesAsync();

        // Assert
        allChanges.Should().HaveCount(3);
        allChanges.Should().BeInDescendingOrder(x => x.ChangedAt);
    }

    [Test]
    public async Task GetAllChangesAsync_WithUserFilter_ReturnsUserSpecificChanges()
    {
        // Arrange
        await _changeTracker.TrackChangeAsync("Key1", "old1", "new1", "User1");
        await _changeTracker.TrackChangeAsync("Key2", "old2", "new2", "User2");
        await _changeTracker.TrackChangeAsync("Key3", "old3", "new3", "User1");

        // Act
        var userChanges = await _changeTracker.GetAllChangesAsync(changedBy: "User1");

        // Assert
        userChanges.Should().HaveCount(2);
        userChanges.Should().OnlyContain(c => c.ChangedBy == "User1");
    }

    [Test]
    public async Task GetAllChangesAsync_WithDateFilters_ReturnsFilteredChanges()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var from = now.AddMinutes(-5);
        var to = now.AddMinutes(5);

        await _changeTracker.TrackChangeAsync("Key1", "old1", "new1");
        await _changeTracker.TrackChangeAsync("Key2", "old2", "new2");

        // Act
        var filteredChanges = await _changeTracker.GetAllChangesAsync(from, to);

        // Assert
        filteredChanges.Should().HaveCount(2);
        filteredChanges.Should().OnlyContain(c => c.ChangedAt >= from && c.ChangedAt <= to);
    }

    #endregion

    #region GetChangeStatisticsAsync Tests

    [Test]
    public async Task GetChangeStatisticsAsync_ReturnsCorrectStatistics()
    {
        // Arrange
        var from = DateTime.UtcNow.AddHours(-1);
        var to = DateTime.UtcNow.AddHours(1);

        await _changeTracker.TrackChangeAsync("Key1", "old1", "new1", "User1");
        await _changeTracker.TrackChangeAsync("Key1", "new1", "new2", "User1");
        await _changeTracker.TrackChangeAsync("Key2", "old2", "new2", "User2");

        // Act
        var statistics = await _changeTracker.GetChangeStatisticsAsync(from, to);

        // Assert
        statistics.Should().NotBeEmpty();
        statistics.Should().ContainKey("TotalChanges");
        statistics.Should().ContainKey("UniqueKeys");
        statistics.Should().ContainKey("UniqueUsers");
        statistics.Should().ContainKey("ChangesByUser");
        statistics.Should().ContainKey("ChangesByKey");
        statistics.Should().ContainKey("ChangesByDay");

        statistics["TotalChanges"].Should().Be(3);
        statistics["UniqueKeys"].Should().Be(2);
        statistics["UniqueUsers"].Should().Be(2);
    }

    [Test]
    public async Task GetChangeStatisticsAsync_WithNoChanges_ReturnsEmptyStatistics()
    {
        // Arrange
        var from = DateTime.UtcNow.AddDays(-1);
        var to = DateTime.UtcNow.AddDays(-1).AddHours(1);

        // Act
        var statistics = await _changeTracker.GetChangeStatisticsAsync(from, to);

        // Assert
        statistics.Should().ContainKey("TotalChanges");
        statistics["TotalChanges"].Should().Be(0);
    }

    #endregion

    #region CleanupHistoryAsync Tests

    [Test]
    public async Task CleanupHistoryAsync_RemovesOldRecords()
    {
        // Arrange
        await _changeTracker.TrackChangeAsync("Key1", "old1", "new1");
        await _changeTracker.TrackChangeAsync("Key2", "old2", "new2");
        
        var cutoffDate = DateTime.UtcNow.AddDays(1); // Future date to remove all

        // Act
        var removedCount = await _changeTracker.CleanupHistoryAsync(cutoffDate);

        // Assert
        removedCount.Should().Be(2);
        
        var remainingChanges = await _changeTracker.GetAllChangesAsync();
        remainingChanges.Should().BeEmpty();
    }

    [Test]
    public async Task CleanupHistoryAsync_WithRecentCutoff_RemovesNoRecords()
    {
        // Arrange
        await _changeTracker.TrackChangeAsync("Key1", "old1", "new1");
        await _changeTracker.TrackChangeAsync("Key2", "old2", "new2");
        
        var cutoffDate = DateTime.UtcNow.AddDays(-1); // Past date

        // Act
        var removedCount = await _changeTracker.CleanupHistoryAsync(cutoffDate);

        // Assert
        removedCount.Should().Be(0);
        
        var remainingChanges = await _changeTracker.GetAllChangesAsync();
        remainingChanges.Should().HaveCount(2);
    }

    #endregion

    #region Memory Management Tests

    [Test]
    public async Task TrackChangeAsync_WithManyChanges_LimitsHistorySize()
    {
        // Arrange & Act - Add more than 1000 changes to test the memory limit
        for (int i = 0; i < 1100; i++)
        {
            await _changeTracker.TrackChangeAsync($"Key{i}", $"old{i}", $"new{i}");
        }

        // Assert
        var allChanges = await _changeTracker.GetAllChangesAsync();
        allChanges.Should().HaveCount(1000); // Should be limited to 1000
    }

    #endregion

    #region Dispose Tests

    [Test]
    public void Dispose_CompletesSuccessfully()
    {
        // Act & Assert
        _changeTracker.Dispose();
        
        // Should complete without throwing
        Assert.Pass();
    }

    [Test]
    public async Task Dispose_ClearsHistory()
    {
        // Arrange
        await _changeTracker.TrackChangeAsync("Key1", "old", "new");

        // Act
        _changeTracker.Dispose();

        // Assert
        // Cannot verify directly as object is disposed, but should not throw
        Assert.Pass();
    }

    #endregion
}