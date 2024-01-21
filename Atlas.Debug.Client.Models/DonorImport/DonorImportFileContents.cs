using System.Collections.Generic;
using Atlas.DonorImport.FileSchema.Models;

namespace Atlas.Debug.Client.Models.DonorImport
{
    /// <summary>
    /// Concrete implementation of the donor import file schema.
    /// </summary>
    public class DonorImportFileContents : DonorImportFileSchema
    {
        /// <inheritdoc/>
        public override UpdateMode updateMode { get; set; }

        /// <inheritdoc/>
        public override IEnumerable<DonorUpdate> donors { get; set; }
    }
}
