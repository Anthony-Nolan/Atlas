using System;

namespace Atlas.HlaMetadataDictionary.ExternalInterface.Models
{
    public class CreationBehaviour
    {
        internal enum Mode
        {
            Latest,
            Active,
            Specific
        }

        internal Mode CreationMode { get; }
        internal string SpecificVersion { get; }

        /// <summary>
        /// When set, new dictionary will always be created, even if one already exists for this version.
        /// Otherwise, will only create dictionary if the specified version does not yet exist.
        /// </summary>
        internal bool ShouldForce { get; }

        private CreationBehaviour(Mode creationMode, bool shouldForce)
        {
            if (creationMode == Mode.Specific)
            {
                throw new InvalidOperationException(
                    "If you wish to Create an HlaMetadataDictionary at a specific version, then you must specify the version wanted");
            }

            CreationMode = creationMode;
            ShouldForce = shouldForce;
        }

        private CreationBehaviour(string specificVersion, bool shouldForce)
        {
            CreationMode = Mode.Specific;
            SpecificVersion = specificVersion;
            ShouldForce = shouldForce;
        }

        public static readonly CreationBehaviour Latest = new CreationBehaviour(Mode.Latest, false);
        public static readonly CreationBehaviour Active = new CreationBehaviour(Mode.Active, true);
        public static CreationBehaviour Specific(string version) => new CreationBehaviour(version, true);
    }
}