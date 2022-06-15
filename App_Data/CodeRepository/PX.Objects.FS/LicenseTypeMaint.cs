using PX.Data;

namespace PX.Objects.FS
{
    public class LicenseTypeMaint : PXGraph<LicenseTypeMaint, FSLicenseType>
    {
        [PXImport(typeof(FSLicenseType))]
        public PXSelect<FSLicenseType> LicenseTypeRecords;

		protected virtual void _(Events.RowDeleting<FSLicenseType> e)
		{
			if (e.Row == null)
				return;

			FSLicense license = PXSelect<FSLicense, Where<FSLicense.licenseTypeID, Equal<Required<FSLicenseType.licenseTypeID>>>>.SelectWindowed(this, 0, 1, e.Row.LicenseTypeID);
			FSServiceLicenseType service = PXSelect<FSServiceLicenseType, Where<FSServiceLicenseType.licenseTypeID, Equal<Required<FSLicenseType.licenseTypeID>>>>.SelectWindowed(this, 0, 1, e.Row.LicenseTypeID);
			if (license != null || service != null)
			{
				throw new PXException(TX.Error.RecordIsReferencedAtLicense);
			}

		}
	}
}
