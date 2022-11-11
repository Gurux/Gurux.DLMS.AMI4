using Gurux.Common.Db;
using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Gurux.DLMS.AMI.Shared.DTOs
{
    /// <summary>
    /// Gurux.DLMS.AMI module version information.
    /// </summary>
    /// 
    [IndexCollection(true, nameof(Module), nameof(Number))]
    public class GXModuleVersion : IUnique<Guid>
    {
        /// <summary>
        /// Version identifier.
        /// </summary>
        public Guid Id
        {
            get;
            set;
        }

        /// <summary>
        /// Is version active.
        /// </summary>
        //Filter uses default value.
        [DefaultValue(true)]
        [Filter(FilterType.Exact)]
        [IsRequired]
        public bool? Active { get; set; }

        /// <summary>
        /// Creation time.
        /// </summary>
        [Index(false, Descend = true)]
        [Filter(FilterType.GreaterOrEqual)]
        public DateTime CreationTime
        {
            get;
            set;
        }

        /// <summary>
        /// Module Version number.
        /// </summary>
        [StringLength(20)]
        [DefaultValue(null)]
        public string? Number
        {
            get;
            set;
        }

        /// <summary>
        /// Is this a pre-release version.
        /// </summary>
        public bool Prerelease
        {
            get;
            set;
        }

        /// <summary>
        /// File path.
        /// </summary>
        public string? FileName
        {
            get;
            set;
        }

        /// <summary>
        /// Version description.
        /// </summary>
        public string? Description
        {
            get;
            set;
        }

        /// <summary>
        /// The module that owns this version.
        /// </summary>
        [DefaultValue(null)]
        [ForeignKey(OnDelete = ForeignKeyDelete.Cascade)]
        [Index(false)]
        public GXModule? Module
        {
            get;
            set;
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return Number;
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        public GXModuleVersion()
        {
            Number = "";
        }
    }
}