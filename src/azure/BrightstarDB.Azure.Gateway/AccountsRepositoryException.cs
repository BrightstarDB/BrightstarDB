using System;

namespace BrightstarDB.Azure.Gateway
{
    public class AccountsRepositoryException : Exception
    {
        /// <summary>
        /// Request attempted to create a new account for a user token
        /// that is already associated with another account
        /// </summary>
        public static int UserAccountExists = 1;

        /// <summary>
        /// Request attempted to add a trial subscrtiption to an account
        /// that already has a trial subscrtipion
        /// </summary>
        public static int AccountHasTrialSubscription = 2;

        /// <summary>
        /// Request attempted to add a store to a subscrtipion that has already reached or
        /// exceeded its StoreCountLimit
        /// </summary>
        public static int StoreCountLimitReached = 3;
        
        /// <summary>
        /// Request specified a user Id that is not assocaiated with an existing account
        /// </summary>
        public static int UserAccountNotFound = 4;

        /// <summary>
        /// Request specified operation on a subscription that either does not exist
        /// or is not managed by the user identified in the same request.
        /// </summary>
        public static int InvalidSubscriptionId = 5;

        public int ErrorCode { get; private set; }

        public AccountsRepositoryException(int code, string msg) : base(msg)
        {
            ErrorCode = code;
        }

    }
}