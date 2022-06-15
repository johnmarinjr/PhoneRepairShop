using System.Collections;
using PX.Objects.PJ.PhotoLogs.PJ.Services;
using PX.Data;
using PX.Objects.CN.Common.Services.DataProviders;
using PX.Objects.PM;

namespace PX.Objects.PJ.PhotoLogs.PJ.GraphExtensions
{
    public abstract class PhotoLogActionsExtensionBase<TGraph, TPrimaryView> : PXGraphExtension<TGraph>
        where TGraph : PXGraph
        where TPrimaryView : class, IBqlTable, new()
    {
        public PXAction<TPrimaryView> DownloadZip;
        public PXAction<TPrimaryView> EmailPhotoLog;

        protected PhotoLogZipServiceBase PhotoLogZipServiceBase;

        [InjectDependency]
        public IProjectDataProvider ProjectDataProvider
        {
            get;
            set;
        }

        [PXUIField(DisplayName = "Export Photo Logs",
            MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
        [PXButton(CommitChanges = true, Category = Messages.Processing)]
        public virtual IEnumerable downloadZip(PXAdapter adapter)
        {
            PhotoLogZipServiceBase.DownloadPhotoLogZip();
            return adapter.Get();
        }

        [PXUIField(DisplayName = "Email Photo Logs",
            MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
        [PXButton(CommitChanges = true, Category = Messages.Processing)]
        public virtual IEnumerable emailPhotoLog(PXAdapter adapter)
        {
            Base.Persist();
            var photoLogZip = PhotoLogZipServiceBase.GetPhotoLogZip();
            var photoLogEmailActivityService = new PhotoLogEmailActivityService(Base, photoLogZip);
            var graph = photoLogEmailActivityService.GetEmailActivityGraph();
            PXRedirectHelper.TryRedirect(graph, PXRedirectHelper.WindowMode.NewWindow);
            return adapter.Get();
        }
    }
}