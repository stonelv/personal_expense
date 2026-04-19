using System.Globalization;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.EntityFrameworkCore;
using PersonalExpense.Application.DTOs;
using PersonalExpense.Application.Exceptions;
using PersonalExpense.Application.Interfaces;
using PersonalExpense.Domain.Entities;
using PersonalExpense.Infrastructure.Data;

namespace PersonalExpense.Application.Services;

public class TransactionService : ITransactionService
{
    private readonly ApplicationDbContext _context;

    public TransactionService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResult<TransactionDto>> GetTransactionsAsync(Guid userId, TransactionFilterParams filter)
    {
        var query = _context.Transactions
            .Include(t => t.Account)
            .Include(t => t.Category)
            .Include(t => t.TransferToAccount)
            .Where(t => t.UserId == userId);

        if (filter.Year.HasValue)
        {
            query = query.Where(t => t.TransactionDate.Year == filter.Year.Value);
        }

        if (filter.Month.HasValue)
        {
            query = query.Where(t => t.TransactionDate.Month == filter.Month.Value);
        }

        if (filter.Type.HasValue)
        {
            query = query.Where(t => t.Type == filter.Type.Value);
        }

        if (filter.AccountId.HasValue)
        {
            query = query.Where(t => t.AccountId == filter.AccountId.Value || 
                                      t.TransferToAccountId == filter.AccountId.Value);
        }

        if (filter.CategoryId.HasValue)
        {
            query = query.Where(t => t.CategoryId == filter.CategoryId.Value);
        }

        if (filter.StartDate.HasValue)
        {
            query = query.Where(t => t.TransactionDate >= filter.StartDate.Value);
        }

        if (filter.EndDate.HasValue)
        {
            query = query.Where(t => t.TransactionDate <= filter.EndDate.Value);
        }

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(t => t.TransactionDate)
            .ThenByDescending(t => t.CreatedAt)
            .Skip((filter.PageNumber - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync();

        return new PagedResult<TransactionDto>
        {
            Items = items.Select(MapToDto).ToList(),
            TotalCount = totalCount,
            PageNumber = filter.PageNumber,
            PageSize = filter.PageSize
        };
    }

    public async Task<TransactionDto?> GetTransactionByIdAsync(Guid id, Guid userId)
    {
        var transaction = await _context.Transactions
            .Include(t => t.Account)
            .Include(t => t.Category)
            .Include(t => t.TransferToAccount)
            .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);

        return transaction != null ? MapToDto(transaction) : null;
    }

    public async Task<TransactionDto> CreateTransactionAsync(TransactionCreateDto dto, Guid userId)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var account = await _context.Accounts
                .FirstOrDefaultAsync(a => a.Id == dto.AccountId && a.UserId == userId);
            
            if (account == null)
            {
                throw new BadRequestException("Account not found");
            }

            if (dto.Type == TransactionType.Transfer && !dto.TransferToAccountId.HasValue)
            {
                throw new BadRequestException("TransferToAccountId is required for transfer transactions");
            }

            if (dto.Type == TransactionType.Transfer && dto.TransferToAccountId.HasValue)
            {
                var toAccount = await _context.Accounts
                    .FirstOrDefaultAsync(a => a.Id == dto.TransferToAccountId.Value && a.UserId == userId);
                
                if (toAccount == null)
                {
                    throw new BadRequestException("Transfer to account not found");
                }
            }

            var newTransaction = new Transaction
            {
                Id = Guid.NewGuid(),
                Type = dto.Type,
                Amount = dto.Amount,
                TransactionDate = dto.TransactionDate,
                Description = dto.Description,
                AttachmentUrl = dto.AttachmentUrl,
                CreatedAt = DateTime.UtcNow,
                UserId = userId,
                AccountId = dto.AccountId,
                CategoryId = dto.CategoryId,
                TransferToAccountId = dto.TransferToAccountId
            };

            _context.Transactions.Add(newTransaction);

            await ApplyTransactionEffectsAsync(newTransaction, account, userId, isCreate: true);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return await GetTransactionByIdAsync(newTransaction.Id, userId) 
                ?? throw new NotFoundException(nameof(Transaction), newTransaction.Id);
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<TransactionDto> UpdateTransactionAsync(Guid id, TransactionUpdateDto dto, Guid userId)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var existingTransaction = await _context.Transactions
                .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);

            if (existingTransaction == null)
            {
                throw new NotFoundException(nameof(Transaction), id);
            }

            var newAccount = await _context.Accounts
                .FirstOrDefaultAsync(a => a.Id == dto.AccountId && a.UserId == userId);
            
            if (newAccount == null)
            {
                throw new BadRequestException("Account not found");
            }

            if (dto.Type == TransactionType.Transfer && !dto.TransferToAccountId.HasValue)
            {
                throw new BadRequestException("TransferToAccountId is required for transfer transactions");
            }

            if (dto.Type == TransactionType.Transfer && dto.TransferToAccountId.HasValue)
            {
                var toAccount = await _context.Accounts
                    .FirstOrDefaultAsync(a => a.Id == dto.TransferToAccountId.Value && a.UserId == userId);
                
                if (toAccount == null)
                {
                    throw new BadRequestException("Transfer to account not found");
                }
            }

            var oldAccount = await _context.Accounts
                .FirstOrDefaultAsync(a => a.Id == existingTransaction.AccountId && a.UserId == userId);
            
            if (oldAccount != null)
            {
                await ApplyTransactionEffectsAsync(existingTransaction, oldAccount, userId, isCreate: false);
            }

            existingTransaction.Type = dto.Type;
            existingTransaction.Amount = dto.Amount;
            existingTransaction.TransactionDate = dto.TransactionDate;
            existingTransaction.Description = dto.Description;
            existingTransaction.AttachmentUrl = dto.AttachmentUrl;
            existingTransaction.UpdatedAt = DateTime.UtcNow;
            existingTransaction.AccountId = dto.AccountId;
            existingTransaction.CategoryId = dto.CategoryId;
            existingTransaction.TransferToAccountId = dto.TransferToAccountId;

            await ApplyTransactionEffectsAsync(existingTransaction, newAccount, userId, isCreate: true);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return await GetTransactionByIdAsync(existingTransaction.Id, userId) 
                ?? throw new NotFoundException(nameof(Transaction), existingTransaction.Id);
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task DeleteTransactionAsync(Guid id, Guid userId)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var existingTransaction = await _context.Transactions
                .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);

            if (existingTransaction == null)
            {
                throw new NotFoundException(nameof(Transaction), id);
            }

            var account = await _context.Accounts
                .FirstOrDefaultAsync(a => a.Id == existingTransaction.AccountId && a.UserId == userId);
            
            if (account != null)
            {
                await ApplyTransactionEffectsAsync(existingTransaction, account, userId, isCreate: false);
            }

            _context.Transactions.Remove(existingTransaction);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    private async Task ApplyTransactionEffectsAsync(Transaction transaction, Account account, Guid userId, bool isCreate)
    {
        var multiplier = isCreate ? 1 : -1;

        switch (transaction.Type)
        {
            case TransactionType.Income:
                account.Balance += transaction.Amount * multiplier;
                break;
            
            case TransactionType.Expense:
                account.Balance -= transaction.Amount * multiplier;
                break;
            
            case TransactionType.Transfer:
                if (!transaction.TransferToAccountId.HasValue)
                {
                    throw new BadRequestException("TransferToAccountId is required for transfer transactions");
                }

                var toAccount = await _context.Accounts
                    .FirstOrDefaultAsync(a => a.Id == transaction.TransferToAccountId.Value && a.UserId == userId);
                
                if (toAccount == null)
                {
                    throw new BadRequestException("Transfer to account not found");
                }

                account.Balance -= transaction.Amount * multiplier;
                toAccount.Balance += transaction.Amount * multiplier;
                break;
        }
    }

    private static TransactionDto MapToDto(Transaction transaction)
    {
        return new TransactionDto(
            transaction.Id,
            transaction.Type,
            transaction.Amount,
            transaction.TransactionDate,
            transaction.Description,
            transaction.AttachmentUrl,
            transaction.CreatedAt,
            transaction.UpdatedAt,
            transaction.AccountId,
            transaction.Account?.Name,
            transaction.CategoryId,
            transaction.Category?.Name,
            transaction.TransferToAccountId,
            transaction.TransferToAccount?.Name
        );
    }

    public async Task<ImportResultDto> ImportTransactionsAsync(Stream csvStream, Guid userId, Guid accountId)
    {
        var result = new ImportResultDto();
        var errors = new List<ImportErrorDto>();
        var skippedRecords = new List<SkippedRecordDto>();
        var validRecords = new List<TransactionImportDto>();

        var account = await _context.Accounts
            .FirstOrDefaultAsync(a => a.Id == accountId && a.UserId == userId);

        if (account == null)
        {
            throw new BadRequestException("Account not found");
        }

        var userCategories = await _context.Categories
            .Where(c => c.UserId == userId && c.IsActive)
            .ToDictionaryAsync(c => c.Name, c => c, StringComparer.OrdinalIgnoreCase);

        using var reader = new StreamReader(csvStream, Encoding.UTF8);
        var csvConfig = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HeaderValidated = null,
            MissingFieldFound = null,
            BadDataFound = context =>
            {
                errors.Add(new ImportErrorDto
                {
                    RowNumber = context.Context.Parser.Row,
                    ErrorMessage = $"无法解析的数据: {context.RawRecord?.Trim()}",
                    RawData = context.RawRecord?.Trim()
                });
            }
        };

        using var csv = new CsvReader(reader, csvConfig);
        var rowNumber = 1;

        try
        {
            await csv.ReadAsync();
            csv.ReadHeader();
            rowNumber++;
        }
        catch (Exception ex)
        {
            errors.Add(new ImportErrorDto
            {
                RowNumber = 1,
                ErrorMessage = $"无法读取CSV表头: {ex.Message}",
                RawData = csv.Context.Parser.RawRecord
            });
            result.Errors = errors;
            result.ErrorCount = errors.Count;
            return result;
        }

        var requiredHeaders = new[] { "日期", "分类", "金额" };
        var missingHeaders = new List<string>();
        var headerRecord = csv.HeaderRecord;
        
        if (headerRecord == null || headerRecord.Length == 0)
        {
            errors.Add(new ImportErrorDto
            {
                RowNumber = 1,
                ErrorMessage = "CSV表头为空或无法识别",
                RawData = csv.Context.Parser.RawRecord
            });
            result.Errors = errors;
            result.ErrorCount = errors.Count;
            return result;
        }

        foreach (var required in requiredHeaders)
        {
            if (!headerRecord.Any(h => h.Equals(required, StringComparison.OrdinalIgnoreCase)))
            {
                missingHeaders.Add(required);
            }
        }

        if (missingHeaders.Any())
        {
            errors.Add(new ImportErrorDto
            {
                RowNumber = 1,
                ErrorMessage = $"缺少必需的表头列: {string.Join(", ", missingHeaders)}。必需列: {string.Join(", ", requiredHeaders)}",
                RawData = string.Join(",", headerRecord)
            });
            result.Errors = errors;
            result.ErrorCount = errors.Count;
            return result;
        }

        var dateFormats = new[] 
        { 
            "yyyy-MM-dd", 
            "yyyy/MM/dd", 
            "MM/dd/yyyy", 
            "MM-dd-yyyy", 
            "dd-MM-yyyy", 
            "yyyy年MM月dd日",
            "yyyyMMdd"
        };

        while (await csv.ReadAsync())
        {
            var currentRow = rowNumber++;
            var rawRecord = csv.Context.Parser.RawRecord;

            try
            {
                var record = csv.GetRecord<CsvTransactionRecord>();
                var validationErrors = new List<string>();

                if (string.IsNullOrWhiteSpace(record?.Date))
                {
                    validationErrors.Add("日期不能为空");
                }

                if (string.IsNullOrWhiteSpace(record?.Category))
                {
                    validationErrors.Add("分类不能为空");
                }

                if (string.IsNullOrWhiteSpace(record?.Amount))
                {
                    validationErrors.Add("金额不能为空");
                }

                if (validationErrors.Any())
                {
                    errors.Add(new ImportErrorDto
                    {
                        RowNumber = currentRow,
                        ErrorMessage = string.Join("; ", validationErrors),
                        RawData = rawRecord?.Trim()
                    });
                    continue;
                }

                if (!DateTime.TryParseExact(record!.Date!.Trim(), dateFormats, 
                    CultureInfo.InvariantCulture, DateTimeStyles.None, out var transactionDate))
                {
                    if (!DateTime.TryParse(record.Date, CultureInfo.InvariantCulture, 
                        DateTimeStyles.None, out transactionDate))
                    {
                        errors.Add(new ImportErrorDto
                        {
                            RowNumber = currentRow,
                            ErrorMessage = $"无效的日期格式: {record.Date}。支持的格式: {string.Join(", ", dateFormats)}",
                            RawData = rawRecord?.Trim()
                        });
                        continue;
                    }
                }

                var amountStr = record.Amount!.Trim().Replace("¥", "").Replace("$", "").Replace(",", "");
                if (!decimal.TryParse(amountStr, NumberStyles.Currency | NumberStyles.Number, 
                    CultureInfo.InvariantCulture, out var amount) || amount <= 0)
                {
                    errors.Add(new ImportErrorDto
                    {
                        RowNumber = currentRow,
                        ErrorMessage = $"无效的金额: {record.Amount}。金额必须是大于0的数字",
                        RawData = rawRecord?.Trim()
                    });
                    continue;
                }

                validRecords.Add(new TransactionImportDto
                {
                    TransactionDate = transactionDate,
                    CategoryName = record.Category!.Trim(),
                    Amount = amount,
                    Description = record.Description?.Trim(),
                    RowNumber = currentRow
                });
            }
            catch (Exception ex)
            {
                errors.Add(new ImportErrorDto
                {
                    RowNumber = currentRow,
                    ErrorMessage = $"解析行数据时出错: {ex.Message}",
                    RawData = rawRecord?.Trim()
                });
            }
        }

        result.TotalRows = validRecords.Count + errors.Count;
        result.ErrorCount = errors.Count;

        if (!validRecords.Any() && !errors.Any())
        {
            result.Errors = errors;
            return result;
        }

        using var dbTransaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var addedCount = 0;
            var skippedCount = 0;
            var newCategoriesToAdd = new List<Category>();

            var existingTransactions = await _context.Transactions
                .Where(t => t.UserId == userId && t.AccountId == accountId)
                .Select(t => new 
                { 
                    t.TransactionDate, 
                    t.Amount, 
                    t.CategoryId, 
                    t.Description 
                })
                .ToListAsync();

            var fileRecordsGroup = validRecords
                .GroupBy(r => new { r.TransactionDate, r.CategoryName, r.Amount, r.Description })
                .ToDictionary(g => g.Key, g => g.ToList());

            foreach (var importRecord in validRecords)
            {
                var categoryName = importRecord.CategoryName;
                Category? category = null;
                
                if (userCategories.TryGetValue(categoryName, out var existingCategory))
                {
                    category = existingCategory;
                }
                else
                {
                    var newCat = newCategoriesToAdd.FirstOrDefault(c => 
                        c.Name.Equals(categoryName, StringComparison.OrdinalIgnoreCase));
                    
                    if (newCat == null)
                    {
                        newCat = new Category
                        {
                            Id = Guid.NewGuid(),
                            Name = categoryName,
                            Type = CategoryType.Expense,
                            IsActive = true,
                            CreatedAt = DateTime.UtcNow,
                            UserId = userId
                        };
                        newCategoriesToAdd.Add(newCat);
                        userCategories[categoryName] = newCat;
                    }
                    category = newCat;
                }

                var groupKey = new { importRecord.TransactionDate, importRecord.CategoryName, importRecord.Amount, importRecord.Description };
                var fileDuplicates = fileRecordsGroup[groupKey];
                
                if (fileDuplicates.IndexOf(importRecord) > 0)
                {
                    skippedCount++;
                    skippedRecords.Add(new SkippedRecordDto
                    {
                        RowNumber = importRecord.RowNumber,
                        Reason = "文件内重复记录",
                        RawData = $"日期: {importRecord.TransactionDate:yyyy-MM-dd}, 分类: {importRecord.CategoryName}, 金额: {importRecord.Amount}"
                    });
                    continue;
                }

                var isHistoryDuplicate = existingTransactions.Any(t =>
                    t.TransactionDate == importRecord.TransactionDate &&
                    t.Amount == importRecord.Amount &&
                    t.CategoryId == category.Id &&
                    t.Description == importRecord.Description);

                if (isHistoryDuplicate)
                {
                    skippedCount++;
                    skippedRecords.Add(new SkippedRecordDto
                    {
                        RowNumber = importRecord.RowNumber,
                        Reason = "历史重复记录",
                        RawData = $"日期: {importRecord.TransactionDate:yyyy-MM-dd}, 分类: {importRecord.CategoryName}, 金额: {importRecord.Amount}"
                    });
                    continue;
                }

                var newTransaction = new Transaction
                {
                    Id = Guid.NewGuid(),
                    Type = TransactionType.Expense,
                    Amount = importRecord.Amount,
                    TransactionDate = importRecord.TransactionDate,
                    Description = importRecord.Description,
                    CreatedAt = DateTime.UtcNow,
                    UserId = userId,
                    AccountId = accountId,
                    CategoryId = category.Id
                };

                _context.Transactions.Add(newTransaction);
                account.Balance -= importRecord.Amount;
                addedCount++;
            }

            if (newCategoriesToAdd.Any())
            {
                _context.Categories.AddRange(newCategoriesToAdd);
            }

            await _context.SaveChangesAsync();
            await dbTransaction.CommitAsync();

            result.AddedCount = addedCount;
            result.SkippedCount = skippedCount;
            result.ErrorCount = errors.Count;
            result.Errors = errors;
            result.SkippedRecords = skippedRecords;
        }
        catch
        {
            await dbTransaction.RollbackAsync();
            throw;
        }

        return result;
    }
}
