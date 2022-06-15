using PX.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace PX.Objects.CR.Extensions.SideBySideComparison
{
	/// <summary>
	/// The set of entities that is used for comparison (see <see cref="CompareEntitiesExt{TGraph,TMain,TComparisonRow}"/>).
	/// </summary>
	public sealed class EntitiesContext
	{
		public EntitiesContext([NotNull] PXGraph graph, [NotNull] EntityEntry mainEntry, [CanBeNull] IEnumerable<EntityEntry> entries)
		{
			Graph = graph ?? throw new ArgumentNullException(nameof(graph));
			MainEntryType = mainEntry?.EntityType ?? throw new ArgumentNullException(nameof(mainEntry));

			var listEntries =
				(entries ?? Enumerable.Empty<EntityEntry>())
				.Prepend(mainEntry)
				.ToList();

			Entries = listEntries.ToDictionary(e => e.EntityType, e => e);
			Tables = listEntries.Select(e => e.EntityType).ToList();
		}

		public EntitiesContext([NotNull] PXGraph graph, [NotNull] EntityEntry mainEntry, [CanBeNull] params EntityEntry[] entries)
			: this(graph, mainEntry, entries?.AsEnumerable())
		{
		}

		/// <summary>
		/// The graph.
		/// </summary>
		public PXGraph Graph { get; }
		/// <summary>
		/// The type of the main item.
		/// </summary>
		/// <remarks>
		/// Corresponds to <see cref="PXGraph.PrimaryItemType"/>).
		/// </remarks>
		public Type MainEntryType { get; }
		/// <summary>
		/// The entry of <see cref="MainEntryType"/>.
		/// </summary>
		public EntityEntry MainEntry => Entries[MainEntryType];
		/// <summary>
		/// All entries in the current context.
		/// </summary>
		public IReadOnlyDictionary<Type, EntityEntry> Entries { get; }
		/// <summary>
		/// All item types that are presented in the current context.
		/// </summary>
		/// <remarks>
		/// Corresponds to the keys of <see cref="Entries"/>.
		/// </remarks>
		public IReadOnlyCollection<Type> Tables { get; }

		/// <summary>
		/// Returns a single entity of the <paramref name="entityType"/> type that is converted to <typeparamref name="T"/>.
		/// </summary>
		/// <typeparam name="T">The generic type of requested entity type.</typeparam>
		/// <param name="entityType">The requested entity type.</param>
		/// <returns>The DAC.</returns>
		/// <exception cref="ArgumentException">
		/// The <paramref name="entityType"/> type is not presented in the <see cref="Entries"/> collection.
		/// </exception>
		/// <exception cref="InvalidCastException">
		/// <see cref="EntityEntry"/> for <paramref name="entityType"/> contains more than one element
		/// or the item cannot be converted to <typeparamref name="T"/>.
		/// </exception>
		public T GetEntity<T>(Type entityType) where T : IBqlTable
			=> this[entityType].Single<T>();

		/// <summary>
		/// Returns a single entity of the <typeparamref name="T"/> type.
		/// </summary>
		/// <typeparam name="T">The generic type of the requested entity type.</typeparam>
		/// <returns>The DAC.</returns>
		/// <exception cref="ArgumentException">
		/// The <typeparamref name="T"/> type is not presented in the <see cref="Entries"/> collection.
		/// </exception>
		/// <exception cref="InvalidCastException">
		/// <see cref="EntityEntry"/> for <typeparamref name="T"/> contains more than one element
		/// or the item cannot be converted to <typeparamref name="T"/>.
		/// </exception>
		public T GetEntity<T>() where T : IBqlTable
			=> GetEntity<T>(typeof(T));

		/// <summary>
		/// Returns the list of entities of the <paramref name="entityType"/> type that are converted to <typeparamref name="T"/>.
		/// </summary>
		/// <typeparam name="T">The generic type of the requested entity type.</typeparam>
		/// <param name="entityType">The requested entity type.</param>
		/// <returns>The DAC.</returns>
		/// <exception cref="ArgumentException">
		/// The <paramref name="entityType"/> type is not presented in the <see cref="Entries"/> collection.
		/// </exception>
		/// <exception cref="InvalidCastException">
		/// At least one item inside <see cref="EntityEntry"/> cannot be converted to <typeparamref name="T"/>.
		/// </exception>
		public IEnumerable<T> GetEntities<T>(Type entityType) where T : IBqlTable
			=> this[entityType].Multiple<T>();

		/// <summary>
		/// Returns the list of entities of the <typeparamref name="T"/> type that are converted to <typeparamref name="T"/>.
		/// </summary>
		/// <typeparam name="T">The generic type of the requested entity type.</typeparam>
		/// <returns>The DAC.</returns>
		/// <exception cref="ArgumentException">
		/// The <typeparamref name="T"/> type is not presented in the <see cref="Entries"/> collection.
		/// </exception>
		/// <exception cref="InvalidCastException">
		/// At least one item inside <see cref="EntityEntry"/> cannot be converted to <typeparamref name="T"/>.
		/// </exception>
		public IEnumerable<T> GetEntities<T>() where T : IBqlTable
			=> GetEntities<T>(typeof(T));

		/// <summary>
		/// Returns the entity entry for the specified item type.
		/// </summary>
		/// <param name="itemType">The item type of the DAC (<see cref="IBqlTable"/>).</param>
		/// <returns>The entity entry.</returns>
		public EntityEntry this[Type itemType] => Entries[itemType];

		/// <summary>
		/// Returns the entity entry for the specified item type.
		/// </summary>
		/// <param name="itemType">The full name of the item type of the DAC (<see cref="IBqlTable"/>).</param>
		/// <returns>The entity entry.</returns>
		public EntityEntry this[string itemType]
		{
			get
			{
				var type = Tables.FirstOrDefault(t => t.FullName == itemType);
				if (type is null)
					throw new ArgumentException("The given key was not present in the dictionary.", nameof(itemType));
				return this[type];
			}
		}

		/// <summary>
		/// Returns the scope that restores all currents (<see cref="PXCache.Current"/>)
		/// for all caches represented by the current entity context on <see cref="IDisposable.Dispose"/>,
		/// so they can be safely changed and restored to the original values.
		/// </summary>
		/// <returns>The scope</returns>
		public IDisposable PreserveCurrentsScope()
		{
			return new ReplaceCurrentScope(Entries.Values.Select(e
				=> new KeyValuePair<PXCache, object>(e.Cache, e.Cache.Current)));
		}
	}
}
