using System.Collections.Generic;

namespace Gurux.DLMS.AMI.Client.Shared
{
    public class GXUpdated 
    {
        /// <summary>
        /// Updated type.
        /// </summary>
        public string Type
        {
            get;
            set;
        }
        /// <summary>
        /// Desctiption can be used in UI.
        /// </summary>
        public string Description
        {
            get;
            set;
        }

        /// <summary>
        /// List of updated IDs.
        /// </summary>
        public IEnumerable<string> Ids
        {
            get;
            set;
        }
    }
}
