﻿using System;
using PX.Common;

namespace PX.Objects.Common.Scopes
{
	public abstract class FlaggedModeScopeBase<FlagType> : IDisposable
	{
		private readonly static string scopeKey;

		static FlaggedModeScopeBase()
		{
			scopeKey = typeof(FlagType).FullName;
		}

		public FlaggedModeScopeBase()
		{
			PXContext.SetSlot(scopeKey, true);
		}

		void IDisposable.Dispose()
		{
			PXContext.SetSlot(scopeKey, false);
		}

		public static bool IsActive => PXContext.GetSlot<bool>(scopeKey);
	}

	public abstract class FlaggedModeScopeBase<FlagType, ParameterType> : IDisposable
	{
		private readonly static string scopeKey;
		private readonly static string parametersKey;
		private const string parametersKeyFormat = "{0}_Parameters";

		static FlaggedModeScopeBase()
		{
			scopeKey = typeof(FlagType).FullName;
			parametersKey = string.Format(parametersKeyFormat, scopeKey);
		}
		public FlaggedModeScopeBase(ParameterType parameters)
		{
			PXContext.SetSlot(scopeKey, true);
			PXContext.SetSlot(parametersKey, parameters);
		}

		void IDisposable.Dispose()
		{
			PXContext.SetSlot(scopeKey, false);
		}

		public static bool IsActive => PXContext.GetSlot<bool>(scopeKey);
		public static ParameterType Parameters => PXContext.GetSlot<ParameterType>(parametersKey);
	}

	public abstract class FlaggedKeyModeScopeBase<FlagType> : IDisposable
	{
		private readonly static string scopeKey;
		private readonly string _key;

		static FlaggedKeyModeScopeBase()
		{
			scopeKey = typeof(FlagType).FullName;
		}

		public FlaggedKeyModeScopeBase(string key)
		{
			_key = scopeKey + key;
			PXContext.SetSlot(_key, true);
		}

		void IDisposable.Dispose()
		{
			PXContext.SetSlot(_key, false);
		}

		public static bool IsActive(string key = "") => PXContext.GetSlot<bool>(scopeKey + key);
	}
}
