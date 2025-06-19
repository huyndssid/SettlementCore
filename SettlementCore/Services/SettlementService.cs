using Microsoft.Extensions.Logging;
using StateMachineCore.Core.StateMachine.Examples;
using StateMachineCore.Models;
using StateMachineCore.Models.Messages;
using StateMachineCore.Services.Interfaces;

namespace StateMachineCore.Services
{
    public class SettlementService : ISettlementService
    {
        private readonly ILogger<SettlementService> _logger;
        private readonly IAssetService _assetService;
        private readonly IWalletService _walletService;
        private readonly IFeeService _feeService;
        private readonly ILedgerService _ledgerService;
        private readonly ISettlementProducer _settlementProducer;

        private StateMachineExample _stateTest;

        public SettlementService(
            ILogger<SettlementService> logger,
            IAssetService assetService,
            IWalletService walletService,
            IFeeService feeService,
            ILedgerService ledgerService,
            ISettlementProducer settlementProducer
            )
        {
            _logger = logger;
            _assetService = assetService;
            _walletService = walletService;
            _feeService = feeService;
            _ledgerService = ledgerService;
            _settlementProducer = settlementProducer;
        }

        public async Task<bool> ProcessSettlementAsync(SettlementTransaction transaction)
        {
            try 
            {
                _logger.LogInformation($"Starting settlement process for trade {transaction.TradeId}");
                transaction.State = SettlementState.Pending;

                // Lock assets
                if (!await LockAssetsAsync(transaction))
                {
                    _logger.LogError($"Failed to lock assets for trade {transaction.TradeId}");
                    await RollbackSettlementAsync(transaction);
                    return false;
                }
                transaction.State = SettlementState.Locked;
                
                // Process transfer
                if (!await ProcessTransferAsync(transaction))
                {
                    _logger.LogError($"Failed to process transfer for trade {transaction.TradeId}");
                    await RollbackSettlementAsync(transaction);
                    return false;
                }
                transaction.State = SettlementState.Processing;
                
                // Process fees
                if (!await ProcessFeesAsync(transaction))
                {
                    _logger.LogError($"Failed to process fees for trade {transaction.TradeId}");
                    await RollbackSettlementAsync(transaction);
                    return false;
                }
                transaction.State = SettlementState.FeeDiscount;
                
                // Complete settlement
                if (!await CompleteSettlementAsync(transaction))
                {
                    _logger.LogError($"Failed to complete settlement for trade {transaction.TradeId}");
                    await RollbackSettlementAsync(transaction);
                    return false;
                }
                transaction.State = SettlementState.Completed;
                
                // Send completion notifications
                await SendCompletionNotificationsAsync(transaction);
                
                _logger.LogInformation($"Successfully completed settlement for trade {transaction.TradeId}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error processing settlement for trade {transaction.TradeId}");
                await RollbackSettlementAsync(transaction);
                return false;
            }
        }

        private async Task SendCompletionNotificationsAsync(SettlementTransaction transaction)
        {
            try
            {
                // Send settlement completed message
                var completedMessage = new SettlementCompletedMessage
                {
                    TradeId = transaction.TradeId,
                    SettlementId = transaction.Id,
                    BuyerId = transaction.BuyerId,
                    SellerId = transaction.SellerId,
                    Symbol = transaction.Symbol,
                    Price = transaction.Price,
                    Quantity = transaction.Quantity,
                    BuyerFee = transaction.BuyerFee,
                    SellerFee = transaction.SellerFee,
                    CompletedAt = DateTime.UtcNow,
                    MakerSide = transaction.MakerSide
                };
                await _settlementProducer.PublishSettlementCompletedAsync(completedMessage);

                // Send balance update messages
                var buyerBalanceMessage = new BalanceUpdateMessage
                {
                    UserId = transaction.BuyerId,
                    Symbol = transaction.Symbol,
                    Balance = await _assetService.GetBalanceAsync(transaction.BuyerId, transaction.Symbol),
                    LockedBalance = 0, // Unlocked after completion
                    UpdatedAt = DateTime.UtcNow,
                    TradeId = transaction.TradeId
                };
                await _settlementProducer.PublishBalanceUpdateAsync(buyerBalanceMessage);

                var sellerBalanceMessage = new BalanceUpdateMessage
                {
                    UserId = transaction.SellerId,
                    Symbol = transaction.Symbol,
                    Balance = await _assetService.GetBalanceAsync(transaction.SellerId, transaction.Symbol),
                    LockedBalance = 0, // Unlocked after completion
                    UpdatedAt = DateTime.UtcNow,
                    TradeId = transaction.TradeId
                };
                await _settlementProducer.PublishBalanceUpdateAsync(sellerBalanceMessage);

                _logger.LogInformation($"Sent completion notifications for trade {transaction.TradeId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send completion notifications for trade {transaction.TradeId}");
                // Don't throw here as settlement is already completed
            }
        }

        private async Task<bool> LockAssetsAsync(SettlementTransaction transaction)
        {
            try
            {
                // Lock buyer's assets
                var buyerLocked = await _assetService.LockAssetsAsync(
                    transaction.BuyerId,
                    transaction.Symbol,
                    transaction.Quantity * transaction.Price);

                if (!buyerLocked)
                {
                    _logger.LogError($"Failed to lock buyer assets for trade {transaction.TradeId}");
                    return false;
                }

                // Lock seller's assets
                var sellerLocked = await _assetService.LockAssetsAsync(
                    transaction.SellerId,
                    transaction.Symbol,
                    transaction.Quantity);

                if (!sellerLocked)
                {
                    // Rollback buyer lock
                    await _assetService.UnlockAssetsAsync(
                        transaction.BuyerId,
                        transaction.Symbol,
                        transaction.Quantity * transaction.Price);
                    
                    _logger.LogError($"Failed to lock seller assets for trade {transaction.TradeId}");
                    return false;
                }

                transaction.IsBuyerLocked = true;
                transaction.IsSellerLocked = true;
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error locking assets for trade {transaction.TradeId}");
                return false;
            }
        }

        private async Task<bool> ProcessTransferAsync(SettlementTransaction transaction)
        {
            try
            {
                // Transfer assets from seller to buyer
                var transferResult = await _walletService.TransferAsync(
                    transaction.SellerId,
                    transaction.BuyerId,
                    transaction.Symbol,
                    transaction.Quantity);

                if (!transferResult)
                {
                    _logger.LogError($"Failed to transfer assets for trade {transaction.TradeId}");
                    return false;
                }

                transaction.IsTransferCompleted = true;
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error processing transfer for trade {transaction.TradeId}");
                return false;
            }
        }

        private async Task<bool> ProcessFeesAsync(SettlementTransaction transaction)
        {
            try
            {
                // Calculate fees
                var (buyerFee, sellerFee) = await _feeService.CalculateFeesAsync(
                    transaction.Symbol,
                    transaction.Price,
                    transaction.Quantity,
                    transaction.MakerSide);

                // Process buyer fee
                var buyerFeeProcessed = await _walletService.DeductFeeAsync(
                    transaction.BuyerId,
                    transaction.Symbol,
                    buyerFee);

                if (!buyerFeeProcessed)
                {
                    _logger.LogError($"Failed to process buyer fee for trade {transaction.TradeId}");
                    return false;
                }

                // Process seller fee
                var sellerFeeProcessed = await _walletService.DeductFeeAsync(
                    transaction.SellerId,
                    transaction.Symbol,
                    sellerFee);

                if (!sellerFeeProcessed)
                {
                    // Rollback buyer fee
                    await _walletService.RefundFeeAsync(
                        transaction.BuyerId,
                        transaction.Symbol,
                        buyerFee);
                    
                    _logger.LogError($"Failed to process seller fee for trade {transaction.TradeId}");
                    return false;
                }

                transaction.BuyerFee = buyerFee;
                transaction.SellerFee = sellerFee;
                transaction.IsFeeProcessed = true;
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error processing fees for trade {transaction.TradeId}");
                return false;
            }
        }

        private async Task<bool> CompleteSettlementAsync(SettlementTransaction transaction)
        {
            try
            {
                // Record transaction in ledger
                var ledgerRecorded = await _ledgerService.RecordTransactionAsync(transaction);
                if (!ledgerRecorded)
                {
                    _logger.LogError($"Failed to record transaction in ledger for trade {transaction.TradeId}");
                    return false;
                }

                // Unlock remaining assets
                await _assetService.UnlockAssetsAsync(
                    transaction.BuyerId,
                    transaction.Symbol,
                    transaction.Quantity * transaction.Price);

                await _assetService.UnlockAssetsAsync(
                    transaction.SellerId,
                    transaction.Symbol,
                    transaction.Quantity);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error completing settlement for trade {transaction.TradeId}");
                return false;
            }
        }

        public async Task<bool> RollbackSettlementAsync(SettlementTransaction transaction)
        {
            try
            {
                _logger.LogInformation($"Starting rollback for trade {transaction.TradeId}");

                if (transaction.IsTransferCompleted)
                {
                    _logger.LogInformation($"Rolling back transfer for trade {transaction.TradeId}");
                    // Reverse the transfer
                    await _walletService.TransferAsync(
                        transaction.BuyerId,
                        transaction.SellerId,
                        transaction.Symbol,
                        transaction.Quantity);
                }

                if (transaction.IsFeeProcessed)
                {
                    _logger.LogInformation($"Rolling back fees for trade {transaction.TradeId}");
                    // Refund fees
                    await _walletService.RefundFeeAsync(
                        transaction.BuyerId,
                        transaction.Symbol,
                        transaction.BuyerFee);

                    await _walletService.RefundFeeAsync(
                        transaction.SellerId,
                        transaction.Symbol,
                        transaction.SellerFee);
                }

                if (transaction.IsBuyerLocked)
                {
                    _logger.LogInformation($"Unlocking buyer assets for trade {transaction.TradeId}");
                    await _assetService.UnlockAssetsAsync(
                        transaction.BuyerId,
                        transaction.Symbol,
                        transaction.Quantity * transaction.Price);
                }

                if (transaction.IsSellerLocked)
                {
                    _logger.LogInformation($"Unlocking seller assets for trade {transaction.TradeId}");
                    await _assetService.UnlockAssetsAsync(
                        transaction.SellerId,
                        transaction.Symbol,
                        transaction.Quantity);
                }

                transaction.State = SettlementState.Failed;

                // Send failure notification
                await SendFailureNotificationAsync(transaction);
                
                _logger.LogInformation($"Successfully completed rollback for trade {transaction.TradeId}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error rolling back settlement for trade {transaction.TradeId}");
                return false;
            }
        }

        private async Task SendFailureNotificationAsync(SettlementTransaction transaction)
        {
            try
            {
                var failedMessage = new SettlementFailedMessage
                {
                    TradeId = transaction.TradeId,
                    SettlementId = transaction.Id,
                    ErrorMessage = transaction.ErrorMessage,
                    FailedAtState = transaction.State,
                    FailedAt = DateTime.UtcNow,
                    RetryCount = transaction.RetryCount
                };
                await _settlementProducer.PublishSettlementFailedAsync(failedMessage);

                _logger.LogInformation($"Sent failure notification for trade {transaction.TradeId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send failure notification for trade {transaction.TradeId}");
                // Don't throw here as rollback is already completed
            }
        }
    }
} 