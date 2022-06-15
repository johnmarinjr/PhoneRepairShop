using PX.Data;

namespace PX.Objects.FS 
{
    public class SkillMaint : PXGraph<SkillMaint, FSSkill>
    {
        [PXImport(typeof(FSSkill))]
        public PXSelect<FSSkill> SkillRecords;

		protected virtual void _(Events.RowDeleting<FSSkill> e)
		{
			if (e.Row == null)
				return;

			FSEmployeeSkill link = PXSelect<FSEmployeeSkill, Where<FSEmployeeSkill.skillID, Equal<Required<FSSkill.skillID>>>>.SelectWindowed(this, 0, 1, e.Row.SkillID);
			FSServiceSkill service = PXSelect<FSServiceSkill, Where<FSServiceSkill.skillID, Equal<Required<FSSkill.skillID>>>>.SelectWindowed(this, 0, 1, e.Row.SkillID);
			if (link != null || service != null)
			{
				throw new PXException(TX.Error.RecordIsReferencedAtEmployee);
			}
		}
	}
}
