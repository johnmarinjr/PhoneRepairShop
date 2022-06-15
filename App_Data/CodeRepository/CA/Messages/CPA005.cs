namespace PX.Objects.Localizations.CA.Messages
{
    [PX.Common.PXLocalizable]
    public class CPA005
    {
        public const string ProviderName = "CPA 005 Export Provider";
        public const string CompressToZipFormat = "Compress to ZIP format";
        public const string FileCreationNumberInvalid = "The Batch Seq. Number '{0}' is invalid. The valid values are between 1 and 9999.";
        public const string CurrencyCodeInvalid = "The currency code {0} is invalid. The valid values are CAD or USD.";
        public const string OriginatorIDEmpty = "The Originator's ID cannot be empty.";
        public const string OriginatorIDTooLong = "The Originator's ID cannot exceed 10 characters.";
        public const string InstitutionIdentNbrEmpty = "The Institutional ID Number cannot be empty.";
        public const string InstitutionIdentNbrInvalid = "The Institutional ID Number {0} is invalid. All characters must be numeric.";
        public const string InstitutionIdentNbrTooLong = "The Institutional ID Number {0} is invalid. The value cannot exceed 3 characters.";
        public const string BatchCannotBeReleased = "This batch has errors and cannot be released.";
        public const string BatchSeqNumberAcceptedValues = "The valid values are between 1 and 9999.";
    }
}
