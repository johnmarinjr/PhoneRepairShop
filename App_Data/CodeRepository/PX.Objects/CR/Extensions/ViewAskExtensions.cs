using System;
using System.Collections.Generic;
using PX.Common;
using PX.Data;

namespace PX.Objects.CR.Extensions
{
	[PXInternalUseOnly]
	public static class ViewAskExtensions
	{
		// currently Cancel doesn't have callback, so it is just a temproraty (I hope) workaround 
		public static WebDialogResult Ask_YesNoCancel_WithCallback(this PXView view, object row, string header, string message, MessageIcon icon)
		{
			var result = view.Ask(
				row,
				header,
				message,
				MessageButtons.AbortRetryIgnore,
				new Dictionary<WebDialogResult, string>
				{
					[WebDialogResult.Abort] = nameof(WebDialogResult.Yes),
					[WebDialogResult.Retry] = nameof(WebDialogResult.No),
					[WebDialogResult.Ignore] = nameof(WebDialogResult.Cancel),
				},
				icon);

			switch (result)
			{
				case WebDialogResult.Abort:
					view.Answer = WebDialogResult.Yes;
					break;
				case WebDialogResult.Retry:
					view.Answer = WebDialogResult.No;
					break;
				case WebDialogResult.Ignore:
					view.Answer = WebDialogResult.Cancel;
					break;
			}

			return view.Answer;
		}

		internal static PXView WithAnswerFor(this PXView view, bool predicate, WebDialogResult answer)
		{
			if (predicate && view.Answer == WebDialogResult.None)
				view.Answer = answer;

			return view;
		}

		#region WithActionIfNoAnswerIf

		public static PXView WithActionIfNoAnswerFor(this PXView view, bool predicate, Action action)
		{
			if (view.Answer == WebDialogResult.None && predicate)
				action();

			return view;
		}
		public static T WithActionIfNoAnswerFor<T>(this T select, bool predicate, Action action) where T : PXSelectBase
		{
			WithActionIfNoAnswerFor(select.View, predicate, action);
			return select;
		}

		#endregion

		#region WithAnswerForCbApi

		/// <summary>
		/// Uses predefined <see cref="WebDialogResult"/> if <see cref="PXView.Answer"/>
		/// is not set (<see cref="WebDialogResult.None"/>)
		/// and the graph in Contact-Based API context (<see cref="PXGraph.IsContractBasedAPI"/>).
		/// </summary>
		/// <param name="view"></param>
		/// <param name="answer"></param>
		/// <returns><paramref name="view"/></returns>
		public static PXView WithAnswerForCbApi(this PXView view, WebDialogResult answer)
		{
			return view.WithAnswerFor(view.Graph.IsContractBasedAPI, answer);
		}

		/// <summary>
		/// Uses predefined <see cref="WebDialogResult"/> if <see cref="PXView.Answer"/>
		/// is not set (<see cref="WebDialogResult.None"/>)
		/// and the graph in Contact-Based API context (<see cref="PXGraph.IsContractBasedAPI"/>).
		/// </summary>
		/// <param name="select"></param>
		/// <param name="answer"></param>
		/// <returns><paramref name="select"/></returns>
		public static T WithAnswerForCbApi<T>(this T select, WebDialogResult answer) where T : PXSelectBase
		{
			WithAnswerForCbApi(select.View, answer);
			return select;
		}

		#endregion

		#region WithAnswerForUnattendedMode

		/// <summary>
		/// Uses predefined <see cref="WebDialogResult"/> if <see cref="PXView.Answer"/>
		/// is not set (<see cref="WebDialogResult.None"/>)
		/// and the graph in unattended mode (<see cref="PXPreserveScope.IsScoped()"/>).
		/// </summary>
		/// <param name="view"></param>
		/// <param name="answer"></param>
		/// <returns><paramref name="view"/></returns>
		public static PXView WithAnswerForUnattendedMode(this PXView view, WebDialogResult answer)
		{
			return view.WithAnswerFor(view.Graph.UnattendedMode, answer);
		}

		/// <summary>
		/// Uses predefined <see cref="WebDialogResult"/> if <see cref="PXView.Answer"/>
		/// is not set (<see cref="WebDialogResult.None"/>)
		/// and the graph in unattended mode (<see cref="PXPreserveScope.IsScoped()"/>).
		/// </summary>
		/// <param name="select"></param>
		/// <param name="answer"></param>
		/// <returns><paramref name="select"/></returns>
		public static T WithAnswerForUnattendedMode<T>(this T select, WebDialogResult answer) where T : PXSelectBase
		{
			WithAnswerForUnattendedMode(select.View, answer);
			return select;
		}

		#endregion

		#region WithAnswerForMobile


		/// <summary>
		/// Uses predefined <see cref="WebDialogResult"/> if <see cref="PXView.Answer"/>
		/// is not set (<see cref="WebDialogResult.None"/>)
		/// and the request came from mobile application (<see cref="PXGraph.IsMobile"/>).
		/// </summary>
		/// <param name="view"></param>
		/// <param name="answer"></param>
		/// <returns><paramref name="view"/></returns>
		public static PXView WithAnswerForMobile(this PXView view, WebDialogResult answer)
		{
			return view.WithAnswerFor(view.Graph.IsMobile, answer);
		}

		/// <summary>
		/// Uses predefined <see cref="WebDialogResult"/> if <see cref="PXView.Answer"/>
		/// is not set (<see cref="WebDialogResult.None"/>)
		/// and the request came from mobile application (<see cref="PXGraph.IsMobile"/>).
		/// </summary>
		/// <param name="select"></param>
		/// <param name="answer"></param>
		/// <returns><paramref name="select"/></returns>
		public static T WithAnswerForMobile<T>(this T select, WebDialogResult answer) where T : PXSelectBase
		{
			WithAnswerForMobile(select.View, answer);
			return select;
		}

		#endregion

		#region WithAnswerForImport


		/// <summary>
		/// Uses predefined <see cref="WebDialogResult"/> if <see cref="PXView.Answer"/>
		/// is not set (<see cref="WebDialogResult.None"/>)
		/// and the request came from import scenario (<see cref="PXGraph.IsImport"/>).
		/// </summary>
		/// <param name="view"></param>
		/// <param name="answer"></param>
		/// <returns><paramref name="view"/></returns>
		public static PXView WithAnswerForImport(this PXView view, WebDialogResult answer)
		{
			return view.WithAnswerFor(view.Graph.IsImport, answer);
		}

		/// <summary>
		/// Uses predefined <see cref="WebDialogResult"/> if <see cref="PXView.Answer"/>
		/// is not set (<see cref="WebDialogResult.None"/>)
		/// and the request came from import scenario (<see cref="PXGraph.IsImport"/>).
		/// </summary>
		/// <param name="select"></param>
		/// <param name="answer"></param>
		/// <returns><paramref name="select"/></returns>
		public static T WithAnswerForImport<T>(this T select, WebDialogResult answer) where T : PXSelectBase
		{
			WithAnswerForImport(select.View, answer);
			return select;
		}

		#endregion
	}
}
