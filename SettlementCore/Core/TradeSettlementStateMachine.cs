using System;
using System.Threading.Tasks;
using Stateless;
using StateMachineCore.Models;
using Microsoft.Extensions.Logging;
using Polly;
using System.Threading;

namespace StateMachineCore.Core
{
    public class TradeSettlementStateMachine
    {
        private readonly StateMachine<SettlementState, SettlementTrigger> _machine;
        private readonly ILogger<TradeSettlementStateMachine> _logger;
        private readonly SettlementTransaction _transaction;
        private readonly IAsyncPolicy _retryPolicy;
        private readonly IAsyncPolicy _circuitBreakerPolicy;

        public TradeSettlementStateMachine(
            SettlementTransaction transaction,
            ILogger<TradeSettlementStateMachine> logger)
        {
            _transaction = transaction;
            _logger = logger;
            _machine = new StateMachine<SettlementState, SettlementTrigger>(
                () => _transaction.State,
                state => _transaction.State = state
            );

            // Configure retry policy
            _retryPolicy = Policy
                .Handle<Exception>()
                .WaitAndRetryAsync(3, retryAttempt => 
                    TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    onRetry: (exception, timeSpan, retryCount, context) =>
                    {
                        _logger.LogWarning(exception, 
                            $"Retry {retryCount} after {timeSpan.TotalSeconds}s for trade {_transaction.TradeId}");
                        _transaction.RetryCount = retryCount;
                    });

            // Configure circuit breaker
            _circuitBreakerPolicy = Policy
                .Handle<Exception>()
                .CircuitBreakerAsync(
                    exceptionsAllowedBeforeBreaking: 2,
                    durationOfBreak: TimeSpan.FromSeconds(30),
                    onBreak: (exception, duration) =>
                    {
                        _logger.LogError(exception, 
                            $"Circuit breaker opened for {duration.TotalSeconds}s for trade {_transaction.TradeId}");
                    },
                    onReset: () =>
                    {
                        _logger.LogInformation($"Circuit breaker reset for trade {_transaction.TradeId}");
                    });

            ConfigureStateMachine();
        }

        private void ConfigureStateMachine()
        {
            // Pending -> Locked
            _machine.Configure(SettlementState.Pending)
                .Permit(SettlementTrigger.LockAssets, SettlementState.Locked)
                .OnEntryAsync(OnPendingEntryAsync);

            // Locked -> Processing
            _machine.Configure(SettlementState.Locked)
                .Permit(SettlementTrigger.ProcessTransfer, SettlementState.Processing)
                .OnEntryAsync(OnLockedEntryAsync);

            // Processing -> FeeDiscount
            _machine.Configure(SettlementState.Processing)
                .Permit(SettlementTrigger.ProcessFees, SettlementState.FeeDiscount)
                .OnEntryAsync(OnProcessingEntryAsync);

            // FeeDiscount -> Completed
            _machine.Configure(SettlementState.FeeDiscount)
                .Permit(SettlementTrigger.Complete, SettlementState.Completed)
                .OnEntryAsync(OnFeeDiscountEntryAsync);

            // Any state -> Failed
            _machine.Configure(SettlementState.Failed)
                .OnEntryAsync(OnFailedEntryAsync);
        }

        private async Task OnPendingEntryAsync()
        {
            _logger.LogInformation($"Processing trade {_transaction.TradeId} in Pending state");
            
            // Validate trade data
            if (string.IsNullOrEmpty(_transaction.TradeId))
                throw new ArgumentException("TradeId is required");
            
            if (string.IsNullOrEmpty(_transaction.BuyerId) || string.IsNullOrEmpty(_transaction.SellerId))
                throw new ArgumentException("BuyerId and SellerId are required");
            
            if (_transaction.Price <= 0 || _transaction.Quantity <= 0)
                throw new ArgumentException("Price and Quantity must be greater than 0");

            // Generate idempotency key
            _transaction.IdempotencyKey = $"settlement:{_transaction.TradeId}:{DateTime.UtcNow.Ticks}";
            
            _logger.LogInformation($"Trade {_transaction.TradeId} validated and ready for processing");
        }

        private async Task OnLockedEntryAsync()
        {
            _logger.LogInformation($"Processing trade {_transaction.TradeId} in Locked state");
            
            // Validate locked state
            if (!_transaction.IsBuyerLocked || !_transaction.IsSellerLocked)
                throw new InvalidOperationException("Both buyer and seller assets must be locked");
            
            _logger.LogInformation($"Assets locked for trade {_transaction.TradeId}");
        }

        private async Task OnProcessingEntryAsync()
        {
            _logger.LogInformation($"Processing trade {_transaction.TradeId} in Processing state");
            
            // Validate transfer state
            if (!_transaction.IsTransferCompleted)
                throw new InvalidOperationException("Transfer must be completed before processing fees");
            
            _logger.LogInformation($"Transfer completed for trade {_transaction.TradeId}");
        }

        private async Task OnFeeDiscountEntryAsync()
        {
            _logger.LogInformation($"Processing trade {_transaction.TradeId} in FeeDiscount state");
            
            // Validate fee processing
            if (!_transaction.IsFeeProcessed)
                throw new InvalidOperationException("Fees must be processed before completion");
            
            if (_transaction.BuyerFee < 0 || _transaction.SellerFee < 0)
                throw new InvalidOperationException("Fees cannot be negative");
            
            _logger.LogInformation($"Fees processed for trade {_transaction.TradeId}");
        }

        private async Task OnFailedEntryAsync()
        {
            _logger.LogError(
                $"Trade {_transaction.TradeId} failed. State: {_transaction.State}, Error: {_transaction.ErrorMessage}, " +
                $"RetryCount: {_transaction.RetryCount}, IsBuyerLocked: {_transaction.IsBuyerLocked}, " +
                $"IsSellerLocked: {_transaction.IsSellerLocked}, IsTransferCompleted: {_transaction.IsTransferCompleted}, " +
                $"IsFeeProcessed: {_transaction.IsFeeProcessed}");
        }

        public async Task FireAsync(SettlementTrigger trigger)
        {
            try
            {
                await _circuitBreakerPolicy.ExecuteAsync(async () =>
                {
                    await _retryPolicy.ExecuteAsync(async () =>
                    {
                        await _machine.FireAsync(trigger);
                    });
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error firing trigger {trigger} for trade {_transaction.TradeId}");
                _transaction.ErrorMessage = ex.Message;
                await _machine.FireAsync(SettlementTrigger.Fail);
            }
        }
    }

    public enum SettlementTrigger
    {
        LockAssets,
        ProcessTransfer,
        ProcessFees,
        Complete,
        Fail
    }
} 