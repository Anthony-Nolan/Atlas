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

        private CreationBehaviour(Mode creationMode)
        {
            if (creationMode == Mode.Specific)
            {
                throw new InvalidOperationException("If you wish to Create an HlaMetadataDictionary at a specific version, then you must specify the version wanted");
            }
            CreationMode = creationMode;
        }

        private CreationBehaviour(string specificVersion)
        {
            CreationMode = Mode.Specific;
            SpecificVersion = specificVersion;
        }

        public static CreationBehaviour Latest = new CreationBehaviour(Mode.Latest);
        public static CreationBehaviour Active = new CreationBehaviour(Mode.Active);
        public static CreationBehaviour Specific(string version) => new CreationBehaviour(version);
    }
}