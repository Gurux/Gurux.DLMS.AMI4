﻿namespace Gurux.DLMS.AMI.Components
{
    /// <summary>
    /// Get items request.
    /// </summary>
    public readonly struct GXItemsProviderRequest
    {
        /// <summary>
        /// The start index of the data segment requested.
        /// </summary>
        public int StartIndex { get; }

        /// <summary>
        /// The requested number of items to be provided. The actual number of provided items does not need to match
        /// this value.
        /// </summary>
        public int Count { get; }

        /// <summary>
        /// Are removed items searched.
        /// </summary>
        public bool Removed { get; }

        /// <summary>
        /// Filter items by string.
        /// </summary>
        public string? Filter
        {
            get;
        }

        /// <summary>
        /// Order by name.
        /// </summary>
        /// <remarks>
        /// Default order by is used if this is not set.
        /// </remarks>
        /// <seealso cref="Descending"/>
        public string? OrderBy
        {
            get;
        }

        /// <summary>
        /// Are values shown as descending order.
        /// </summary>
        /// <seealso cref="OrderBy"/>
        public bool Descending
        {
            get;
        }

        /// <summary>
        /// Data from the all users is shown for the admin.
        /// </summary>
        public bool ShowAllUserData
        {
            get;
        }


        /// <summary>
        /// The <see cref="System.Threading.CancellationToken"/> used to relay cancellation of the request.
        /// </summary>
        public CancellationToken CancellationToken { get; }

        /// <summary>
        /// Constructs a new <see cref="GXItemsProviderRequest"/> instance.
        /// </summary>
        /// <param name="startIndex">The start index of the data segment requested.</param>
        /// <param name="count">The requested number of items to be provided.</param>
        /// <param name="showAllUserData">Data from the all users is shown for the admin.</param>
        /// <param name="removed">Are removed items searched.</param>
        /// <param name="orderBy">Default order by is used if this is not set.</param>
        /// <param name="descending">Are values shown as descending order.</param>
        /// <param name="filter">Used filter.</param>
        /// <param name="cancellationToken">The <see cref="System.Threading.CancellationToken"/> used to relay cancellation of the request.</param>
        public GXItemsProviderRequest(int startIndex,
            int count,
            bool showAllUserData = true,
            bool removed = false,
            string? orderBy = null,
            bool descending = false,
            string? filter = null,
            CancellationToken cancellationToken = default)
        {
            StartIndex = startIndex;
            ShowAllUserData = showAllUserData;
            Count = count;
            OrderBy = orderBy;
            Descending = descending;
            Filter = filter;
            CancellationToken = cancellationToken;
            Removed = removed;
        }
    }
}