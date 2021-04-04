using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using MongoDB.Driver;
using OK.Bitter.Common.Entities;
using OK.Bitter.DataAccess.DataContexts;

namespace OK.Bitter.DataAccess.HostedServices
{
    public class SeedHostedService : IHostedService
    {
        private readonly BitterDataContext _context;

        public SeedHostedService(BitterDataContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await _context.Alerts.Indexes.CreateManyAsync(
                 new[]
                 {
                    new CreateIndexModel<AlertEntity>(
                        Builders<AlertEntity>.IndexKeys.Ascending(x => x.UserId),
                        new CreateIndexOptions { Name = "UserId_1" }),
                    new CreateIndexModel<AlertEntity>(
                        Builders<AlertEntity>.IndexKeys.Ascending(x => x.SymbolId),
                        new CreateIndexOptions { Name = "SymbolId_1" }),
                    new CreateIndexModel<AlertEntity>(
                        Builders<AlertEntity>.IndexKeys.Ascending(x => x.UserId).Ascending(x => x.SymbolId),
                        new CreateIndexOptions { Name = "UserId_1_SymbolId_1" })
                 },
                 cancellationToken);

            await _context.Messages.Indexes.CreateManyAsync(
                  new[]
                  {
                     new CreateIndexModel<MessageEntity>(
                        Builders<MessageEntity>.IndexKeys.Ascending(x => x.UserId),
                        new CreateIndexOptions { Name = "UserId_1_SymbolId_1" })
                  },
                  cancellationToken);

            await _context.Prices.Indexes.CreateManyAsync(
                new[]
                {
                    new CreateIndexModel<PriceEntity>(
                        Builders<PriceEntity>.IndexKeys.Ascending(x => x.SymbolId),
                        new CreateIndexOptions { Name = "SymbolId_1" }),
                    new CreateIndexModel<PriceEntity>(
                        Builders<PriceEntity>.IndexKeys.Descending(x => x.Date),
                        new CreateIndexOptions { Name = "Date_-1" }),
                    new CreateIndexModel<PriceEntity>(
                        Builders<PriceEntity>.IndexKeys.Ascending(x => x.SymbolId).Descending(x => x.Date),
                        new CreateIndexOptions { Name = "SymbolId_1_Date_-1" })
                },
                cancellationToken);

            await _context.Settings.Indexes.CreateManyAsync(
                new[]
                {
                    new CreateIndexModel<SettingEntity>(
                        Builders<SettingEntity>.IndexKeys.Ascending(x => x.UserId),
                        new CreateIndexOptions { Name = "UserId_1" }),
                    new CreateIndexModel<SettingEntity>(
                        Builders<SettingEntity>.IndexKeys.Ascending(x => x.Key),
                        new CreateIndexOptions { Name = "Key_1" }),
                    new CreateIndexModel<SettingEntity>(
                        Builders<SettingEntity>.IndexKeys.Ascending(x => x.UserId).Ascending(x => x.Key),
                        new CreateIndexOptions { Name = "UserId_1_Key_1" })
                 },
                 cancellationToken);

            await _context.Subscriptions.Indexes.CreateManyAsync(
                new[]
                {
                    new CreateIndexModel<SubscriptionEntity>(
                        Builders<SubscriptionEntity>.IndexKeys.Ascending(x => x.UserId),
                        new CreateIndexOptions { Name = "UserId_1" }),
                    new CreateIndexModel<SubscriptionEntity>(
                        Builders<SubscriptionEntity>.IndexKeys.Ascending(x => x.SymbolId),
                        new CreateIndexOptions { Name = "SymbolId_1" }),
                    new CreateIndexModel<SubscriptionEntity>(
                        Builders<SubscriptionEntity>.IndexKeys.Ascending(x => x.UserId).Ascending(x => x.SymbolId),
                        new CreateIndexOptions { Name = "UserId_1_SymbolId_1" })
                },
                cancellationToken);

            await _context.Symbols.Indexes.CreateManyAsync(
                new[]
                {
                    new CreateIndexModel<SymbolEntity>(
                        Builders<SymbolEntity>.IndexKeys.Ascending(x => x.Name),
                        new CreateIndexOptions { Name = "Name_1" }),
                    new CreateIndexModel<SymbolEntity>(
                        Builders<SymbolEntity>.IndexKeys.Ascending(x => x.FriendlyName),
                        new CreateIndexOptions { Name = "FriendlyName_1" })
                },
                 cancellationToken);

            await _context.Trades.Indexes.CreateManyAsync(
                 new[]
                 {
                    new CreateIndexModel<TradeEntity>(
                        Builders<TradeEntity>.IndexKeys.Ascending(x => x.UserId),
                        new CreateIndexOptions { Name = "UserId_1" }),
                    new CreateIndexModel<TradeEntity>(
                        Builders<TradeEntity>.IndexKeys.Ascending(x => x.SymbolId),
                        new CreateIndexOptions { Name = "SymbolId_1" }),
                    new CreateIndexModel<TradeEntity>(
                        Builders<TradeEntity>.IndexKeys.Ascending(x => x.UserId).Ascending(x => x.SymbolId),
                        new CreateIndexOptions { Name = "UserId_1_SymbolId_1" })
                 },
                 cancellationToken);

            await _context.Users.Indexes.CreateManyAsync(
                new[]
                {
                    new CreateIndexModel<UserEntity>(
                        Builders<UserEntity>.IndexKeys.Ascending(x => x.ChatId),
                        new CreateIndexOptions { Name = "ChatId_1" }),
                    new CreateIndexModel<UserEntity>(
                        Builders<UserEntity>.IndexKeys.Ascending(x => x.Username),
                        new CreateIndexOptions { Name = "Username_1" })
                },
                cancellationToken);
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}