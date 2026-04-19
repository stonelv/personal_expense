using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Moq;
using PersonalExpense.Application.DTOs;
using PersonalExpense.Application.Exceptions;
using PersonalExpense.Application.Interfaces;
using PersonalExpense.Application.Services;
using PersonalExpense.Domain.Entities;
using PersonalExpense.Infrastructure.Data;

namespace PersonalExpense.Tests;

public class SubscriptionServiceTests
{
    private readonly Guid _userId = Guid.NewGuid();
    private readonly ApplicationDbContext _context;
    private readonly Mock<ITransactionService> _transactionServiceMock;
    private readonly SubscriptionService _service;

    public SubscriptionServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(b => b.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _context = new ApplicationDbContext(options);
        _transactionServiceMock = new Mock<ITransactionService>();
        _service = new SubscriptionService(_context, _transactionServiceMock.Object);
    }

    private async Task<Account> CreateTestAccountAsync(string name, decimal initialBalance)
    {
        var account = new Account
        {
            Id = Guid.NewGuid(),
            Name = name,
            Type = AccountType.Cash,
            Balance = initialBalance,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UserId = _userId
        };

        _context.Accounts.Add(account);
        await _context.SaveChangesAsync();
        return account;
    }

    private async Task<Category> CreateTestCategoryAsync(string name, CategoryType type)
    {
        var category = new Category
        {
            Id = Guid.NewGuid(),
            Name = name,
            Type = type,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UserId = _userId
        };

        _context.Categories.Add(category);
        await _context.SaveChangesAsync();
        return category;
    }

    #region Create Tests

    [Fact]
    public async Task CreateSubscription_WithValidData_ShouldCreateSuccessfully()
    {
        // Arrange
        var account = await CreateTestAccountAsync("Cash", 1000);
        var category = await CreateTestCategoryAsync("Subscription", CategoryType.Expense);

        var dto = new SubscriptionCreateDto(
            Name: "Netflix",
            Amount: 99,
            Type: TransactionType.Expense,
            Frequency: SubscriptionFrequency.Monthly,
            StartDate: DateTime.UtcNow.AddDays(-5),
            EndDate: null,
            Description: "Monthly subscription",
            AccountId: account.Id,
            CategoryId: category.Id
        );

        // Act
        var result = await _service.CreateSubscriptionAsync(dto, _userId);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("Netflix");
        result.Amount.Should().Be(99);
        result.Type.Should().Be(TransactionType.Expense);
        result.Frequency.Should().Be(SubscriptionFrequency.Monthly);
        result.Status.Should().Be(SubscriptionStatus.Active);
        result.AccountId.Should().Be(account.Id);
        result.CategoryId.Should().Be(category.Id);

        var subscription = await _context.Subscriptions.FindAsync(result.Id);
        subscription.Should().NotBeNull();
        subscription!.UserId.Should().Be(_userId);
    }

    [Fact]
    public async Task CreateSubscription_WithInvalidAccount_ShouldThrowException()
    {
        // Arrange
        var invalidAccountId = Guid.NewGuid();
        var dto = new SubscriptionCreateDto(
            Name: "Netflix",
            Amount: 99,
            Type: TransactionType.Expense,
            Frequency: SubscriptionFrequency.Monthly,
            StartDate: DateTime.UtcNow,
            EndDate: null,
            Description: "Monthly subscription",
            AccountId: invalidAccountId,
            CategoryId: null
        );

        // Act & Assert
        var exception = await Assert.ThrowsAsync<BadRequestException>(
            () => _service.CreateSubscriptionAsync(dto, _userId));

        exception.Message.Should().Contain("Account not found");

        var subscriptions = await _context.Subscriptions.ToListAsync();
        subscriptions.Should().BeEmpty();
    }

    [Fact]
    public async Task CreateSubscription_WithEndDateBeforeStartDate_ShouldThrowException()
    {
        // Arrange
        var account = await CreateTestAccountAsync("Cash", 1000);

        var dto = new SubscriptionCreateDto(
            Name: "Netflix",
            Amount: 99,
            Type: TransactionType.Expense,
            Frequency: SubscriptionFrequency.Monthly,
            StartDate: DateTime.UtcNow,
            EndDate: DateTime.UtcNow.AddDays(-10),
            Description: "Monthly subscription",
            AccountId: account.Id,
            CategoryId: null
        );

        // Act & Assert
        var exception = await Assert.ThrowsAsync<BadRequestException>(
            () => _service.CreateSubscriptionAsync(dto, _userId));

        exception.Message.Should().Contain("End date cannot be earlier than start date");
    }

    [Fact]
    public async Task CreateSubscription_WithPastStartDate_ShouldCalculateCorrectNextDueDate()
    {
        // Arrange
        var account = await CreateTestAccountAsync("Cash", 1000);
        var pastDate = DateTime.UtcNow.AddMonths(-2);

        var dto = new SubscriptionCreateDto(
            Name: "Netflix",
            Amount: 99,
            Type: TransactionType.Expense,
            Frequency: SubscriptionFrequency.Monthly,
            StartDate: pastDate,
            EndDate: null,
            Description: "Monthly subscription",
            AccountId: account.Id,
            CategoryId: null
        );

        // Act
        var result = await _service.CreateSubscriptionAsync(dto, _userId);

        // Assert
        result.Should().NotBeNull();
        result.NextDueDate.Should().BeAfter(DateTime.UtcNow);
        result.NextDueDate.Month.Should().Be(DateTime.UtcNow.AddMonths(1).Month);
    }

    #endregion

    #region Update Tests

    [Fact]
    public async Task UpdateSubscription_WithValidData_ShouldUpdateSuccessfully()
    {
        // Arrange
        var account = await CreateTestAccountAsync("Cash", 1000);
        var category = await CreateTestCategoryAsync("Subscription", CategoryType.Expense);

        var createDto = new SubscriptionCreateDto(
            Name: "Netflix",
            Amount: 99,
            Type: TransactionType.Expense,
            Frequency: SubscriptionFrequency.Monthly,
            StartDate: DateTime.UtcNow.AddDays(-5),
            EndDate: null,
            Description: "Monthly subscription",
            AccountId: account.Id,
            CategoryId: category.Id
        );

        var created = await _service.CreateSubscriptionAsync(createDto, _userId);

        var updateDto = new SubscriptionUpdateDto(
            Name: "Netflix Premium",
            Amount: 149,
            Type: TransactionType.Expense,
            Frequency: SubscriptionFrequency.Monthly,
            StartDate: DateTime.UtcNow.AddDays(-5),
            EndDate: null,
            Status: SubscriptionStatus.Active,
            Description: "Premium subscription",
            AccountId: account.Id,
            CategoryId: category.Id
        );

        // Act
        var result = await _service.UpdateSubscriptionAsync(created.Id, updateDto, _userId);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("Netflix Premium");
        result.Amount.Should().Be(149);
        result.Description.Should().Be("Premium subscription");
        result.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task UpdateSubscription_WithNonExistentId_ShouldThrowException()
    {
        // Arrange
        var account = await CreateTestAccountAsync("Cash", 1000);
        var nonExistentId = Guid.NewGuid();

        var updateDto = new SubscriptionUpdateDto(
            Name: "Netflix",
            Amount: 99,
            Type: TransactionType.Expense,
            Frequency: SubscriptionFrequency.Monthly,
            StartDate: DateTime.UtcNow,
            EndDate: null,
            Status: SubscriptionStatus.Active,
            Description: "Monthly subscription",
            AccountId: account.Id,
            CategoryId: null
        );

        // Act & Assert
        var exception = await Assert.ThrowsAsync<NotFoundException>(
            () => _service.UpdateSubscriptionAsync(nonExistentId, updateDto, _userId));

        exception.Message.Should().Contain(nameof(Subscription));
    }

    #endregion

    #region Delete Tests

    [Fact]
    public async Task DeleteSubscription_WithValidId_ShouldDeleteSuccessfully()
    {
        // Arrange
        var account = await CreateTestAccountAsync("Cash", 1000);

        var createDto = new SubscriptionCreateDto(
            Name: "Netflix",
            Amount: 99,
            Type: TransactionType.Expense,
            Frequency: SubscriptionFrequency.Monthly,
            StartDate: DateTime.UtcNow,
            EndDate: null,
            Description: "Monthly subscription",
            AccountId: account.Id,
            CategoryId: null
        );

        var created = await _service.CreateSubscriptionAsync(createDto, _userId);

        // Act
        await _service.DeleteSubscriptionAsync(created.Id, _userId);

        // Assert
        var deleted = await _context.Subscriptions.FindAsync(created.Id);
        deleted.Should().BeNull();
    }

    [Fact]
    public async Task DeleteSubscription_WithNonExistentId_ShouldThrowException()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<NotFoundException>(
            () => _service.DeleteSubscriptionAsync(nonExistentId, _userId));

        exception.Message.Should().Contain(nameof(Subscription));
    }

    #endregion

    #region Get Tests

    [Fact]
    public async Task GetSubscriptions_WithPagination_ShouldReturnCorrectPage()
    {
        // Arrange
        var account = await CreateTestAccountAsync("Cash", 1000);

        for (int i = 0; i < 25; i++)
        {
            var dto = new SubscriptionCreateDto(
                Name: $"Subscription {i}",
                Amount: 100 + i,
                Type: TransactionType.Expense,
                Frequency: SubscriptionFrequency.Monthly,
                StartDate: DateTime.UtcNow.AddDays(-i),
                EndDate: null,
                Description: $"Description {i}",
                AccountId: account.Id,
                CategoryId: null
            );
            await _service.CreateSubscriptionAsync(dto, _userId);
        }

        // Act
        var filter = new SubscriptionFilterParams
        {
            PageNumber = 2,
            PageSize = 10
        };
        var result = await _service.GetSubscriptionsAsync(_userId, filter);

        // Assert
        result.Should().NotBeNull();
        result.TotalCount.Should().Be(25);
        result.PageNumber.Should().Be(2);
        result.PageSize.Should().Be(10);
        result.Items.Count.Should().Be(10);
        result.TotalPages.Should().Be(3);
        result.HasPrevious.Should().BeTrue();
        result.HasNext.Should().BeTrue();
    }

    [Fact]
    public async Task GetSubscriptions_WithStatusFilter_ShouldReturnOnlyMatching()
    {
        // Arrange
        var account = await CreateTestAccountAsync("Cash", 1000);

        var activeDto = new SubscriptionCreateDto(
            Name: "Active Subscription",
            Amount: 99,
            Type: TransactionType.Expense,
            Frequency: SubscriptionFrequency.Monthly,
            StartDate: DateTime.UtcNow,
            EndDate: null,
            Description: "Active",
            AccountId: account.Id,
            CategoryId: null
        );
        var active = await _service.CreateSubscriptionAsync(activeDto, _userId);

        // Act
        var filter = new SubscriptionFilterParams
        {
            Status = SubscriptionStatus.Active,
            PageNumber = 1,
            PageSize = 100
        };
        var result = await _service.GetSubscriptionsAsync(_userId, filter);

        // Assert
        result.Should().NotBeNull();
        result.TotalCount.Should().Be(1);
        result.Items.All(s => s.Status == SubscriptionStatus.Active).Should().BeTrue();
    }

    [Fact]
    public async Task GetSubscriptionById_WithValidId_ShouldReturnSubscription()
    {
        // Arrange
        var account = await CreateTestAccountAsync("Cash", 1000);
        var category = await CreateTestCategoryAsync("Subscription", CategoryType.Expense);

        var createDto = new SubscriptionCreateDto(
            Name: "Netflix",
            Amount: 99,
            Type: TransactionType.Expense,
            Frequency: SubscriptionFrequency.Monthly,
            StartDate: DateTime.UtcNow,
            EndDate: null,
            Description: "Monthly subscription",
            AccountId: account.Id,
            CategoryId: category.Id
        );

        var created = await _service.CreateSubscriptionAsync(createDto, _userId);

        // Act
        var result = await _service.GetSubscriptionByIdAsync(created.Id, _userId);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("Netflix");
        result.AccountName.Should().Be(account.Name);
        result.CategoryName.Should().Be(category.Name);
    }

    #endregion

    #region Reminder Tests

    [Fact]
    public async Task GetUpcomingReminders_WithDueIn3Days_ShouldReturnReminder()
    {
        // Arrange
        var account = await CreateTestAccountAsync("Cash", 1000);

        var subscription = new Subscription
        {
            Id = Guid.NewGuid(),
            Name = "Netflix",
            Amount = 99,
            Type = TransactionType.Expense,
            Frequency = SubscriptionFrequency.Monthly,
            StartDate = DateTime.UtcNow,
            NextDueDate = DateTime.UtcNow.AddDays(2),
            Status = SubscriptionStatus.Active,
            AccountId = account.Id,
            UserId = _userId,
            CreatedAt = DateTime.UtcNow
        };

        _context.Subscriptions.Add(subscription);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetUpcomingRemindersAsync(_userId, daysInAdvance: 3);

        // Assert
        result.Should().NotBeNull();
        result.Count.Should().Be(1);
        result[0].SubscriptionName.Should().Be("Netflix");
        result[0].DaysUntilDue.Should().Be(2);
    }

    [Fact]
    public async Task GetUpcomingReminders_WithDueIn5Days_ShouldNotReturnReminder()
    {
        // Arrange
        var account = await CreateTestAccountAsync("Cash", 1000);

        var subscription = new Subscription
        {
            Id = Guid.NewGuid(),
            Name = "Netflix",
            Amount = 99,
            Type = TransactionType.Expense,
            Frequency = SubscriptionFrequency.Monthly,
            StartDate = DateTime.UtcNow,
            NextDueDate = DateTime.UtcNow.AddDays(5),
            Status = SubscriptionStatus.Active,
            AccountId = account.Id,
            UserId = _userId,
            CreatedAt = DateTime.UtcNow
        };

        _context.Subscriptions.Add(subscription);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetUpcomingRemindersAsync(_userId, daysInAdvance: 3);

        // Assert
        result.Should().NotBeNull();
        result.Count.Should().Be(0);
    }

    [Fact]
    public async Task GetUpcomingReminders_WithInactiveSubscription_ShouldNotReturnReminder()
    {
        // Arrange
        var account = await CreateTestAccountAsync("Cash", 1000);

        var subscription = new Subscription
        {
            Id = Guid.NewGuid(),
            Name = "Netflix",
            Amount = 99,
            Type = TransactionType.Expense,
            Frequency = SubscriptionFrequency.Monthly,
            StartDate = DateTime.UtcNow,
            NextDueDate = DateTime.UtcNow.AddDays(2),
            Status = SubscriptionStatus.Paused,
            AccountId = account.Id,
            UserId = _userId,
            CreatedAt = DateTime.UtcNow
        };

        _context.Subscriptions.Add(subscription);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetUpcomingRemindersAsync(_userId, daysInAdvance: 3);

        // Assert
        result.Should().NotBeNull();
        result.Count.Should().Be(0);
    }

    #endregion

    #region Record Payment Tests

    [Fact]
    public async Task RecordSubscriptionPayment_WithValidData_ShouldCreateTransaction()
    {
        // Arrange
        var account = await CreateTestAccountAsync("Cash", 1000);
        var category = await CreateTestCategoryAsync("Subscription", CategoryType.Expense);

        var createDto = new SubscriptionCreateDto(
            Name: "Netflix",
            Amount: 99,
            Type: TransactionType.Expense,
            Frequency: SubscriptionFrequency.Monthly,
            StartDate: DateTime.UtcNow.AddDays(-30),
            EndDate: null,
            Description: "Monthly subscription",
            AccountId: account.Id,
            CategoryId: category.Id
        );

        var created = await _service.CreateSubscriptionAsync(createDto, _userId);

        var expectedTransactionDto = new TransactionDto(
            Id: Guid.NewGuid(),
            Type: TransactionType.Expense,
            Amount: 99,
            TransactionDate: DateTime.UtcNow,
            Description: "Netflix",
            AttachmentUrl: null,
            CreatedAt: DateTime.UtcNow,
            UpdatedAt: null,
            AccountId: account.Id,
            AccountName: account.Name,
            CategoryId: category.Id,
            CategoryName: category.Name,
            TransferToAccountId: null,
            TransferToAccountName: null
        );

        _transactionServiceMock
            .Setup(s => s.CreateTransactionAsync(It.IsAny<TransactionCreateDto>(), _userId))
            .ReturnsAsync(expectedTransactionDto);

        var paymentDto = new RecordSubscriptionPaymentDto(
            PaymentDate: DateTime.UtcNow,
            Description: "Paid for March",
            AttachmentUrl: null
        );

        // Act
        var result = await _service.RecordSubscriptionPaymentAsync(created.Id, paymentDto, _userId);

        // Assert
        result.Should().NotBeNull();

        var updatedSubscription = await _context.Subscriptions.FindAsync(created.Id);
        updatedSubscription.Should().NotBeNull();
        updatedSubscription!.LastPaidDate.Should().Be(paymentDto.PaymentDate);
        updatedSubscription.NextDueDate.Should().BeAfter(created.NextDueDate);
        updatedSubscription.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task RecordSubscriptionPayment_WithInactiveSubscription_ShouldThrowException()
    {
        // Arrange
        var account = await CreateTestAccountAsync("Cash", 1000);

        var subscription = new Subscription
        {
            Id = Guid.NewGuid(),
            Name = "Netflix",
            Amount = 99,
            Type = TransactionType.Expense,
            Frequency = SubscriptionFrequency.Monthly,
            StartDate = DateTime.UtcNow,
            NextDueDate = DateTime.UtcNow.AddDays(5),
            Status = SubscriptionStatus.Paused,
            AccountId = account.Id,
            UserId = _userId,
            CreatedAt = DateTime.UtcNow
        };

        _context.Subscriptions.Add(subscription);
        await _context.SaveChangesAsync();

        var paymentDto = new RecordSubscriptionPaymentDto(
            PaymentDate: DateTime.UtcNow,
            Description: null,
            AttachmentUrl: null
        );

        // Act & Assert
        var exception = await Assert.ThrowsAsync<BadRequestException>(
            () => _service.RecordSubscriptionPaymentAsync(subscription.Id, paymentDto, _userId));

        exception.Message.Should().Contain("inactive subscription");
    }

    #endregion

    #region Frequency Calculation Tests

    [Theory]
    [InlineData(SubscriptionFrequency.Daily, 1)]
    [InlineData(SubscriptionFrequency.Weekly, 7)]
    [InlineData(SubscriptionFrequency.Monthly, 30)]
    public async Task CreateSubscription_WithDifferentFrequencies_ShouldCalculateCorrectNextDueDate(
        SubscriptionFrequency frequency,
        int daysToAdd)
    {
        // Arrange
        var account = await CreateTestAccountAsync("Cash", 1000);
        var pastDate = DateTime.UtcNow.AddDays(-10);

        var dto = new SubscriptionCreateDto(
            Name: $"Test {frequency}",
            Amount: 100,
            Type: TransactionType.Expense,
            Frequency: frequency,
            StartDate: pastDate,
            EndDate: null,
            Description: $"Test {frequency} subscription",
            AccountId: account.Id,
            CategoryId: null
        );

        // Act
        var result = await _service.CreateSubscriptionAsync(dto, _userId);

        // Assert
        result.Should().NotBeNull();
        result.NextDueDate.Should().BeAfter(DateTime.UtcNow);
    }

    [Fact]
    public async Task CreateSubscription_WithEndDate_ShouldNotExceedEndDate()
    {
        // Arrange
        var account = await CreateTestAccountAsync("Cash", 1000);

        var dto = new SubscriptionCreateDto(
            Name: "Limited Subscription",
            Amount: 99,
            Type: TransactionType.Expense,
            Frequency: SubscriptionFrequency.Monthly,
            StartDate: DateTime.UtcNow.AddMonths(-2),
            EndDate: DateTime.UtcNow.AddMonths(1),
            Description: "Limited time subscription",
            AccountId: account.Id,
            CategoryId: null
        );

        // Act
        var result = await _service.CreateSubscriptionAsync(dto, _userId);

        // Assert
        result.Should().NotBeNull();
        result.NextDueDate.Should().BeOnOrBefore(result.EndDate!.Value);
    }

    #endregion

    #region Update NextDueDate Preservation Tests

    [Fact]
    public async Task UpdateSubscription_WithoutFrequencyChange_ShouldPreserveNextDueDate()
    {
        // Arrange
        var account = await CreateTestAccountAsync("Cash", 1000);

        var createDto = new SubscriptionCreateDto(
            Name: "Netflix",
            Amount: 99,
            Type: TransactionType.Expense,
            Frequency: SubscriptionFrequency.Monthly,
            StartDate: DateTime.UtcNow.AddDays(-30),
            EndDate: null,
            Description: "Monthly subscription",
            AccountId: account.Id,
            CategoryId: null
        );

        var created = await _service.CreateSubscriptionAsync(createDto, _userId);
        var originalNextDueDate = created.NextDueDate;

        var updateDto = new SubscriptionUpdateDto(
            Name: "Netflix Premium",
            Amount: 149,
            Type: TransactionType.Expense,
            Frequency: SubscriptionFrequency.Monthly,
            StartDate: DateTime.UtcNow.AddDays(-30),
            EndDate: null,
            Status: SubscriptionStatus.Active,
            Description: "Premium subscription",
            AccountId: account.Id,
            CategoryId: null
        );

        // Act
        var result = await _service.UpdateSubscriptionAsync(created.Id, updateDto, _userId);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("Netflix Premium");
        result.Amount.Should().Be(149);
        result.NextDueDate.Should().Be(originalNextDueDate);
    }

    [Fact]
    public async Task UpdateSubscription_WithFrequencyChange_ShouldRecalculateNextDueDate()
    {
        // Arrange
        var account = await CreateTestAccountAsync("Cash", 1000);

        var createDto = new SubscriptionCreateDto(
            Name: "Netflix",
            Amount: 99,
            Type: TransactionType.Expense,
            Frequency: SubscriptionFrequency.Monthly,
            StartDate: DateTime.UtcNow.AddDays(-30),
            EndDate: null,
            Description: "Monthly subscription",
            AccountId: account.Id,
            CategoryId: null
        );

        var created = await _service.CreateSubscriptionAsync(createDto, _userId);
        var originalNextDueDate = created.NextDueDate;

        var updateDto = new SubscriptionUpdateDto(
            Name: "Netflix",
            Amount: 99,
            Type: TransactionType.Expense,
            Frequency: SubscriptionFrequency.Weekly,
            StartDate: DateTime.UtcNow.AddDays(-30),
            EndDate: null,
            Status: SubscriptionStatus.Active,
            Description: "Weekly subscription",
            AccountId: account.Id,
            CategoryId: null
        );

        // Act
        var result = await _service.UpdateSubscriptionAsync(created.Id, updateDto, _userId);

        // Assert
        result.Should().NotBeNull();
        result.Frequency.Should().Be(SubscriptionFrequency.Weekly);
        result.NextDueDate.Should().NotBe(originalNextDueDate);
    }

    #endregion

    #region Generate Upcoming Transactions Tests

    [Fact]
    public async Task GenerateUpcomingTransactions_WithActiveSubscriptions_ShouldCreatePendingTransactions()
    {
        // Arrange
        var account = await CreateTestAccountAsync("Cash", 1000);

        var subscription = new Subscription
        {
            Id = Guid.NewGuid(),
            Name = "Netflix",
            Amount = 99,
            Type = TransactionType.Expense,
            Frequency = SubscriptionFrequency.Monthly,
            StartDate = DateTime.UtcNow.AddMonths(-2),
            NextDueDate = DateTime.UtcNow.AddDays(5),
            Status = SubscriptionStatus.Active,
            AccountId = account.Id,
            UserId = _userId,
            CreatedAt = DateTime.UtcNow
        };

        _context.Subscriptions.Add(subscription);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GenerateUpcomingTransactionsAsync(_userId);

        // Assert
        result.Should().NotBeNull();
        result.Count.Should().Be(1);
        result[0].SubscriptionId.Should().Be(subscription.Id);
        result[0].IsGeneratedFromSubscription.Should().BeTrue();
        result[0].Description.Should().Contain("待记账");

        var transaction = await _context.Transactions.FirstOrDefaultAsync(t => t.SubscriptionId == subscription.Id);
        transaction.Should().NotBeNull();
        transaction!.IsGeneratedFromSubscription.Should().BeTrue();
        transaction.Amount.Should().Be(99);
    }

    [Fact]
    public async Task GenerateUpcomingTransactions_WithInactiveSubscriptions_ShouldNotCreateTransactions()
    {
        // Arrange
        var account = await CreateTestAccountAsync("Cash", 1000);

        var pausedSubscription = new Subscription
        {
            Id = Guid.NewGuid(),
            Name = "Netflix",
            Amount = 99,
            Type = TransactionType.Expense,
            Frequency = SubscriptionFrequency.Monthly,
            StartDate = DateTime.UtcNow.AddMonths(-2),
            NextDueDate = DateTime.UtcNow.AddDays(5),
            Status = SubscriptionStatus.Paused,
            AccountId = account.Id,
            UserId = _userId,
            CreatedAt = DateTime.UtcNow
        };

        var cancelledSubscription = new Subscription
        {
            Id = Guid.NewGuid(),
            Name = "Spotify",
            Amount = 15,
            Type = TransactionType.Expense,
            Frequency = SubscriptionFrequency.Monthly,
            StartDate = DateTime.UtcNow.AddMonths(-3),
            NextDueDate = DateTime.UtcNow.AddDays(10),
            Status = SubscriptionStatus.Cancelled,
            AccountId = account.Id,
            UserId = _userId,
            CreatedAt = DateTime.UtcNow
        };

        _context.Subscriptions.Add(pausedSubscription);
        _context.Subscriptions.Add(cancelledSubscription);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GenerateUpcomingTransactionsAsync(_userId);

        // Assert
        result.Should().NotBeNull();
        result.Count.Should().Be(0);

        var transactions = await _context.Transactions.Where(t => t.UserId == _userId).ToListAsync();
        transactions.Result.Count.Should().Be(0);
    }

    #endregion

    #region Idempotency Tests

    [Fact]
    public async Task GenerateUpcomingTransactions_CalledMultipleTimes_ShouldBeIdempotent()
    {
        // Arrange
        var account = await CreateTestAccountAsync("Cash", 1000);

        var subscription = new Subscription
        {
            Id = Guid.NewGuid(),
            Name = "Netflix",
            Amount = 99,
            Type = TransactionType.Expense,
            Frequency = SubscriptionFrequency.Monthly,
            StartDate = DateTime.UtcNow.AddMonths(-2),
            NextDueDate = DateTime.UtcNow.AddDays(5),
            Status = SubscriptionStatus.Active,
            AccountId = account.Id,
            UserId = _userId,
            CreatedAt = DateTime.UtcNow
        };

        _context.Subscriptions.Add(subscription);
        await _context.SaveChangesAsync();

        // Act
        var result1 = await _service.GenerateUpcomingTransactionsAsync(_userId);
        var result2 = await _service.GenerateUpcomingTransactionsAsync(_userId);
        var result3 = await _service.GenerateUpcomingTransactionsAsync(_userId);

        // Assert
        result1.Should().NotBeNull();
        result1.Count.Should().Be(1);

        result2.Should().NotBeNull();
        result2.Count.Should().Be(0);

        result3.Should().NotBeNull();
        result3.Count.Should().Be(0);

        var transactions = await _context.Transactions
            .Where(t => t.SubscriptionId == subscription.Id)
            .ToListAsync();

        transactions.Count.Should().Be(1);
    }

    [Fact]
    public async Task GenerateUpcomingTransactions_MultipleSubscriptions_ShouldGenerateEachOnce()
    {
        // Arrange
        var account = await CreateTestAccountAsync("Cash", 1000);

        var subscription1 = new Subscription
        {
            Id = Guid.NewGuid(),
            Name = "Netflix",
            Amount = 99,
            Type = TransactionType.Expense,
            Frequency = SubscriptionFrequency.Monthly,
            StartDate = DateTime.UtcNow.AddMonths(-2),
            NextDueDate = DateTime.UtcNow.AddDays(5),
            Status = SubscriptionStatus.Active,
            AccountId = account.Id,
            UserId = _userId,
            CreatedAt = DateTime.UtcNow
        };

        var subscription2 = new Subscription
        {
            Id = Guid.NewGuid(),
            Name = "Spotify",
            Amount = 15,
            Type = TransactionType.Expense,
            Frequency = SubscriptionFrequency.Monthly,
            StartDate = DateTime.UtcNow.AddMonths(-3),
            NextDueDate = DateTime.UtcNow.AddDays(10),
            Status = SubscriptionStatus.Active,
            AccountId = account.Id,
            UserId = _userId,
            CreatedAt = DateTime.UtcNow
        };

        _context.Subscriptions.Add(subscription1);
        _context.Subscriptions.Add(subscription2);
        await _context.SaveChangesAsync();

        // Act
        var result1 = await _service.GenerateUpcomingTransactionsAsync(_userId);
        var result2 = await _service.GenerateUpcomingTransactionsAsync(_userId);

        // Assert
        result1.Should().NotBeNull();
        result1.Count.Should().Be(2);

        result2.Should().NotBeNull();
        result2.Count.Should().Be(0);

        var transactions = await _context.Transactions
            .Where(t => t.UserId == _userId)
            .OrderBy(t => t.Amount)
            .ToListAsync();

        transactions.Count.Should().Be(2);
        transactions[0].Amount.Should().Be(15);
        transactions[1].Amount.Should().Be(99);
    }

    #endregion

    #region Different User Tests

    [Fact]
    public async Task GenerateUpcomingTransactions_DifferentUser_ShouldNotGenerateForOtherUser()
    {
        // Arrange
        var account = await CreateTestAccountAsync("Cash", 1000);
        var otherUserId = Guid.NewGuid();

        var subscription = new Subscription
        {
            Id = Guid.NewGuid(),
            Name = "Netflix",
            Amount = 99,
            Type = TransactionType.Expense,
            Frequency = SubscriptionFrequency.Monthly,
            StartDate = DateTime.UtcNow.AddMonths(-2),
            NextDueDate = DateTime.UtcNow.AddDays(5),
            Status = SubscriptionStatus.Active,
            AccountId = account.Id,
            UserId = otherUserId,
            CreatedAt = DateTime.UtcNow
        };

        _context.Subscriptions.Add(subscription);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GenerateUpcomingTransactionsAsync(_userId);

        // Assert
        result.Should().NotBeNull();
        result.Count.Should().Be(0);

        var transactions = await _context.Transactions.ToListAsync();
        transactions.Count.Should().Be(0);
    }

    #endregion
}
