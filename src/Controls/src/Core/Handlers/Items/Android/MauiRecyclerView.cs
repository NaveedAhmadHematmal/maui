﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using Android.Content;
using AndroidX.RecyclerView.Widget;
using Microsoft.Maui.Controls.Internals;
using Microsoft.Maui.Controls.Platform;
using Microsoft.Maui.Graphics;
using ARect = Android.Graphics.Rect;
using AViewCompat = AndroidX.Core.View.ViewCompat;

namespace Microsoft.Maui.Controls.Handlers.Items
{

	public class MauiRecyclerView<TItemsView, TAdapter, TItemsViewSource> : RecyclerView, IMauiRecyclerView<TItemsView>
		where TItemsView : ItemsView
		where TAdapter : ItemsViewAdapter<TItemsView, TItemsViewSource>
		where TItemsViewSource : IItemsViewSource
	{
		protected TAdapter ItemsViewAdapter;

		protected TItemsView ItemsView;
		protected IItemsLayout ItemsLayout { get; private set; }

		Func<IItemsLayout> GetItemsLayout;
		Func<TAdapter> CreateAdapter;

		SnapManager _snapManager;
		ScrollHelper _scrollHelper;
		RecyclerViewScrollListener<TItemsView, TItemsViewSource> _recyclerViewScrollListener;

		EmptyViewAdapter _emptyViewAdapter;
		readonly DataChangeObserver _emptyCollectionObserver;
		readonly DataChangeObserver _itemsUpdateScrollObserver;

		ScrollBarVisibility _defaultHorizontalScrollVisibility = ScrollBarVisibility.Default;
		ScrollBarVisibility _defaultVerticalScrollVisibility = ScrollBarVisibility.Default;

		ItemDecoration _itemDecoration;

		ItemTouchHelper _itemTouchHelper;
		SimpleItemTouchHelperCallback _itemTouchHelperCallback;

		public MauiRecyclerView(Context context, Func<IItemsLayout> getItemsLayout, Func<TAdapter> getAdapter) : base(context)
		{
			GetItemsLayout = getItemsLayout ?? throw new ArgumentNullException(nameof(getItemsLayout));
			CreateAdapter = getAdapter ?? throw new ArgumentNullException(nameof(getAdapter));

			_emptyCollectionObserver = new DataChangeObserver(UpdateEmptyViewVisibility);
			_itemsUpdateScrollObserver = new DataChangeObserver(AdjustScrollForItemUpdate);

			VerticalScrollBarEnabled = false;
			HorizontalScrollBarEnabled = false;
		}

		public void TearDownOldElement(TItemsView oldElement)
		{
			// Stop listening for layout property changes
			if (ItemsLayout != null)
			{
				ItemsLayout.PropertyChanged -= LayoutPropertyChanged;
			}

			// Stop listening for ScrollTo requests
			oldElement.ScrollToRequested -= ScrollToRequested;

			RemoveScrollListener();

			if (ItemsViewAdapter != null)
			{
				// Stop watching for empty items or scroll adjustments
				_emptyCollectionObserver.Stop(ItemsViewAdapter);
				_itemsUpdateScrollObserver.Stop(ItemsViewAdapter);

				// Unhook whichever adapter is active
				SetAdapter(null);

				_emptyViewAdapter?.Dispose();
				ItemsViewAdapter?.Dispose();
			}

			if (_snapManager != null)
			{
				_snapManager.Dispose();
				_snapManager = null;
			}

			if (_itemDecoration != null)
			{
				RemoveItemDecoration(_itemDecoration);
			}

			if (_itemTouchHelper != null)
			{
				_itemTouchHelper.AttachToRecyclerView(null);
				_itemTouchHelper.Dispose();
				_itemTouchHelper = null;
			}

			if (_itemTouchHelperCallback != null)
			{
				_itemTouchHelperCallback.Dispose();
				_itemTouchHelperCallback = null;
			}
		}

		public void SetUpNewElement(TItemsView newElement)
		{
			if (newElement == null)
			{
				ItemsView = null;
				return;
			}

			ItemsView = newElement;

			UpdateBackgroundColor();
			UpdateBackground();

			// Listen for ScrollTo requests
			ItemsView.ScrollToRequested += ScrollToRequested;

			// Listen for ScrollTo requests
			AddOrUpdateScrollListener();

			// Update the snap behavior after add the scroll listener
			UpdateSnapBehavior();
		}

		public void UpdateItemTemplate()
		{
			GetRecycledViewPool().Clear();
			UpdateAdapter();
		}

		public void UpdateScrollingMode()
		{
			UpdateItemsUpdatingScrollMode();
		}

		public virtual void UpdateVerticalScrollBarVisibility()
		{
			if (_defaultVerticalScrollVisibility == ScrollBarVisibility.Default)
				_defaultVerticalScrollVisibility = VerticalScrollBarEnabled ? ScrollBarVisibility.Always : ScrollBarVisibility.Never;

			var newVerticalScrollVisibility = ItemsView.VerticalScrollBarVisibility;

			if (newVerticalScrollVisibility == ScrollBarVisibility.Default)
				newVerticalScrollVisibility = _defaultVerticalScrollVisibility;

			VerticalScrollBarEnabled = newVerticalScrollVisibility == ScrollBarVisibility.Always;
		}

		public virtual void UpdateHorizontalScrollBarVisibility()
		{
			if (_defaultHorizontalScrollVisibility == ScrollBarVisibility.Default)
				_defaultHorizontalScrollVisibility =
					HorizontalScrollBarEnabled ? ScrollBarVisibility.Always : ScrollBarVisibility.Never;

			var newHorizontalScrollVisiblility = ItemsView.HorizontalScrollBarVisibility;

			if (newHorizontalScrollVisiblility == ScrollBarVisibility.Default)
				newHorizontalScrollVisiblility = _defaultHorizontalScrollVisibility;

			HorizontalScrollBarEnabled = newHorizontalScrollVisiblility == ScrollBarVisibility.Always;
		}

		public virtual void UpdateEmptyView()
		{
			if (ItemsViewAdapter == null || ItemsView == null)
			{
				return;
			}

			var emptyView = ItemsView?.EmptyView;
			var emptyViewTemplate = ItemsView?.EmptyViewTemplate;

			if (emptyView != null || emptyViewTemplate != null)
			{
				if (_emptyViewAdapter == null)
				{
					_emptyViewAdapter = new EmptyViewAdapter(ItemsView);
				}

				if (ItemsView is StructuredItemsView structuredItemsView)
				{
					_emptyViewAdapter.Header = structuredItemsView.Header;
					_emptyViewAdapter.HeaderTemplate = structuredItemsView.HeaderTemplate;

					_emptyViewAdapter.Footer = structuredItemsView.Footer;
					_emptyViewAdapter.FooterTemplate = structuredItemsView.FooterTemplate;
				}

				_emptyViewAdapter.EmptyView = emptyView;
				_emptyViewAdapter.EmptyViewTemplate = emptyViewTemplate;

				_emptyCollectionObserver.Start(ItemsViewAdapter);

				_emptyViewAdapter.NotifyDataSetChanged();
			}
			else
			{
				_emptyCollectionObserver.Stop(ItemsViewAdapter);
			}

			UpdateEmptyViewVisibility();
		}

		public virtual void UpdateFlowDirection()
		{
			if (ItemsView == null)
			{
				return;
			}

			this.UpdateFlowDirection(ItemsView);

			ReconcileFlowDirectionAndLayout();
		}

		public virtual void UpdateAdapter()
		{
			var oldItemViewAdapter = ItemsViewAdapter;

			ItemsViewAdapter = CreateAdapter();

			if (GetAdapter() != _emptyViewAdapter)
			{
				_emptyCollectionObserver.Stop(oldItemViewAdapter);
				_itemsUpdateScrollObserver.Stop(oldItemViewAdapter);

				SetAdapter(null);

				SwapAdapter(ItemsViewAdapter, true);
			}

			UpdateEmptyView();

			_itemTouchHelperCallback?.SetAdapter(ItemsViewAdapter as IItemTouchHelperAdapter);

			oldItemViewAdapter?.Dispose();
		}

		public virtual void UpdateCanReorderItems()
		{
			var canReorderItems = (ItemsView as ReorderableItemsView)?.CanReorderItems == true;

			if (canReorderItems)
			{
				if (_itemTouchHelperCallback == null)
				{
					_itemTouchHelperCallback = new SimpleItemTouchHelperCallback();
				}
				if (_itemTouchHelper == null)
				{
					_itemTouchHelper = new ItemTouchHelper(_itemTouchHelperCallback);
					_itemTouchHelper.AttachToRecyclerView(this);
				}
				_itemTouchHelperCallback.SetAdapter(ItemsViewAdapter as IItemTouchHelperAdapter);
			}
			else
			{
				if (_itemTouchHelper != null)
				{
					_itemTouchHelper.AttachToRecyclerView(null);
					_itemTouchHelper.Dispose();
					_itemTouchHelper = null;
				}
				if (_itemTouchHelperCallback != null)
				{
					_itemTouchHelperCallback.Dispose();
					_itemTouchHelperCallback = null;
				}
			}
		}

		public virtual void UpdateLayoutManager()
		{
			if (ItemsLayout != null)
				ItemsLayout.PropertyChanged -= LayoutPropertyChanged;

			ItemsLayout = GetItemsLayout();

			// Keep track of the ItemsLayout's property changes
			if (ItemsLayout != null)
				ItemsLayout.PropertyChanged += LayoutPropertyChanged;

			SetLayoutManager(SelectLayoutManager(ItemsLayout));

			UpdateFlowDirection();
			UpdateItemSpacing();
		}


		protected virtual RecyclerViewScrollListener<TItemsView, TItemsViewSource> CreateScrollListener()
			=> new(ItemsView, ItemsViewAdapter);

		protected virtual void UpdateSnapBehavior()
		{
			_snapManager = GetSnapManager();

			_snapManager.UpdateSnapBehavior(ItemsLayout);
		}

		protected virtual SnapManager GetSnapManager()
		{
			if (_snapManager == null)
				_snapManager = new SnapManager(this);
			return _snapManager;
		}

		// TODO hartez 2018/08/09 09:30:17 Package up background color and flow direction providers so we don't have to re-implement them here	
		protected virtual void UpdateBackgroundColor(Color color = null)
		{
			if (ItemsView == null)
				return;

			var backgroundColor = color ?? ItemsView.BackgroundColor;

			if (backgroundColor == null)
				return;

			SetBackgroundColor(backgroundColor.ToPlatform());
		}

		protected virtual void UpdateBackground(Brush brush = null)
		{
			if (ItemsView == null)
				return;

			if (!(this is RecyclerView recyclerView))
				return;

			Brush background = ItemsView.Background;

			recyclerView.UpdateBackground(background);
		}

		public virtual void UpdateItemsSource()
		{
			if (ItemsView == null)
			{
				return;
			}

			// Stop watching the old adapter 
			var adapter = ItemsViewAdapter ?? GetAdapter();
			_emptyCollectionObserver.Stop(adapter);
			_itemsUpdateScrollObserver.Stop(adapter);

			UpdateAdapter();

			// Set up any properties which require observing data changes in the adapter
			UpdateItemsUpdatingScrollMode();

			UpdateEmptyView();
			AddOrUpdateScrollListener();
			UpdateSnapBehavior();
		}

		protected virtual void UpdateItemsUpdatingScrollMode()
		{
			if (ItemsViewAdapter == null || ItemsView == null)
				return;

			if (ItemsView.ItemsUpdatingScrollMode == ItemsUpdatingScrollMode.KeepItemsInView)
			{
				// Keeping the current items in view is the default, so we don't need to watch for data changes
				_itemsUpdateScrollObserver.Stop(ItemsViewAdapter);
			}
			else
			{
				_itemsUpdateScrollObserver.Start(ItemsViewAdapter);
			}
		}

		protected virtual void ScrollTo(ScrollToRequestEventArgs args)
		{
			if (ItemsView == null)
				return;

			var position = DetermineTargetPosition(args);

			if (args.IsAnimated)
			{
				ScrollHelper.AnimateScrollToPosition(position, args.ScrollToPosition);
			}
			else
			{
				ScrollHelper.JumpScrollToPosition(position, args.ScrollToPosition);
			}
		}

		protected virtual LayoutManager SelectLayoutManager(IItemsLayout layoutSpecification)
		{
			switch (layoutSpecification)
			{
				case GridItemsLayout gridItemsLayout:
					return CreateGridLayout(gridItemsLayout);
				case LinearItemsLayout listItemsLayout:
					var orientation = listItemsLayout.Orientation == ItemsLayoutOrientation.Horizontal
						? LinearLayoutManager.Horizontal
						: LinearLayoutManager.Vertical;

					return new LinearLayoutManager(Context, orientation, false);
			}

			// Fall back to plain old vertical list
			// TODO hartez 2018/08/30 19:34:36 Log a warning when we have to fall back because of an unknown layout	
			return new LinearLayoutManager(Context);
		}

		protected virtual int DetermineTargetPosition(ScrollToRequestEventArgs args)
		{
			if (args.Mode == ScrollToMode.Position)
			{
				// TODO hartez 2018/08/28 15:40:03 Need to handle group indices here as well	
				return args.Index;
			}

			return ItemsViewAdapter.GetPositionForItem(args.Item);
		}

		protected virtual void UpdateItemSpacing()
		{
			if (ItemsLayout == null)
			{
				return;
			}

			if (_itemDecoration != null)
			{
				RemoveItemDecoration(_itemDecoration);
			}

			_itemDecoration = CreateSpacingDecoration(ItemsLayout);
			AddItemDecoration(_itemDecoration);

			if (_itemDecoration is SpacingItemDecoration spacingDecoration)
			{
				// SpacingItemDecoration applies spacing to all items & all 4 sides of the items.
				// We need to adjust the padding on the RecyclerView so this spacing isn't visible around the outer edge of our control.
				// Horizontal & vertical spacing should only exist between items. 
				var horizontalPadding = -spacingDecoration.HorizontalOffset;
				var verticalPadding = -spacingDecoration.VerticalOffset;
				SetPadding(horizontalPadding, verticalPadding, horizontalPadding, verticalPadding);
			}
		}

		protected virtual ItemDecoration CreateSpacingDecoration(IItemsLayout itemsLayout)
		{
			return new SpacingItemDecoration(Context, itemsLayout);
		}

		protected virtual void ReconcileFlowDirectionAndLayout()
		{
			if (!(GetLayoutManager() is LinearLayoutManager linearLayoutManager))
			{
				return;
			}

			if (linearLayoutManager.CanScrollVertically())
			{
				return;
			}
		}

		protected virtual void ScrollToRequested(object sender, ScrollToRequestEventArgs args)
		{
			(GetSnapManager()?.GetCurrentSnapHelper() as SingleSnapHelper)?.ResetCurrentTargetPosition();
			ScrollTo(args);
		}

		protected virtual void LayoutPropertyChanged(object sender, PropertyChangedEventArgs propertyChanged)
		{
			if (propertyChanged.Is(GridItemsLayout.SpanProperty))
			{
				if (GetLayoutManager() is GridLayoutManager gridLayoutManager)
				{
					gridLayoutManager.SpanCount = ((GridItemsLayout)ItemsLayout).Span;
				}
			}
			else if (propertyChanged.IsOneOf(Microsoft.Maui.Controls.ItemsLayout.SnapPointsTypeProperty, Microsoft.Maui.Controls.ItemsLayout.SnapPointsAlignmentProperty))
			{
				UpdateSnapBehavior();
			}
			else if (propertyChanged.IsOneOf(LinearItemsLayout.ItemSpacingProperty,
				GridItemsLayout.HorizontalItemSpacingProperty, GridItemsLayout.VerticalItemSpacingProperty))
			{
				UpdateItemSpacing();
			}
		}

		protected override void OnLayout(bool changed, int l, int t, int r, int b)
		{
			base.OnLayout(changed, l, t, r, b);
			AViewCompat.SetClipBounds(this, new ARect(0, 0, Width, Height));

			// After a direct (non-animated) scroll operation, we may need to make adjustments
			// to align the target item; if an adjustment is pending, execute it here.
			// (Deliberately checking the private member here rather than the property accessor; the accessor will
			// create a new ScrollHelper if needed, and there's no reason to do that until a Scroll is requested.)
			_scrollHelper?.AdjustScroll();
		}

		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);
			if (disposing)
			{
				TearDownOldElement(ItemsView);
			}
		}

		internal ScrollHelper ScrollHelper => _scrollHelper ??= new ScrollHelper(this);

		internal void UpdateEmptyViewVisibility()
		{
			if (ItemsViewAdapter == null)
			{
				return;
			}

			int itemCount = 0;
			if (ItemsView is StructuredItemsView itemsView)
			{
				if (itemsView.Header != null || itemsView.HeaderTemplate != null)
					itemCount++;
				if (itemsView.Footer != null || itemsView.FooterTemplate != null)
					itemCount++;
			}

			var showEmptyView = ItemsView?.EmptyView != null && ItemsViewAdapter.ItemCount == itemCount;

			var currentAdapter = GetAdapter();
			if (showEmptyView && currentAdapter != _emptyViewAdapter)
			{
				SwapAdapter(_emptyViewAdapter, true);

				// TODO hartez 2018/10/24 17:34:36 If this works, cache this layout manager as _emptyLayoutManager	
				SetLayoutManager(new LinearLayoutManager(Context));
			}
			else if (!showEmptyView && currentAdapter != ItemsViewAdapter)
			{
				SwapAdapter(ItemsViewAdapter, true);
				UpdateLayoutManager();
			}
		}

		internal void AdjustScrollForItemUpdate()
		{
			if (ItemsView.ItemsUpdatingScrollMode == ItemsUpdatingScrollMode.KeepLastItemInView)
			{
				ScrollTo(new ScrollToRequestEventArgs(GetLayoutManager().ItemCount, 0,
					Microsoft.Maui.Controls.ScrollToPosition.MakeVisible, true));
			}
			else if (ItemsView.ItemsUpdatingScrollMode == ItemsUpdatingScrollMode.KeepScrollOffset)
			{
				ScrollHelper.UndoNextScrollAdjustment();
			}
		}

		GridLayoutManager CreateGridLayout(GridItemsLayout gridItemsLayout)
		{
			var gridLayoutManager = new GridLayoutManager(Context, gridItemsLayout.Span,
				gridItemsLayout.Orientation == ItemsLayoutOrientation.Horizontal
					? LinearLayoutManager.Horizontal
					: LinearLayoutManager.Vertical,
				false);

			// Give the layout a way to determine that headers/footers span multiple rows/columns
			gridLayoutManager.SetSpanSizeLookup(new GridLayoutSpanSizeLookup(gridItemsLayout, this));

			return gridLayoutManager;
		}

		void AddOrUpdateScrollListener()
		{
			RemoveScrollListener();

			_recyclerViewScrollListener = CreateScrollListener();
			AddOnScrollListener(_recyclerViewScrollListener);
		}

		void RemoveScrollListener()
		{
			if (_recyclerViewScrollListener == null)
				return;

			_recyclerViewScrollListener.Dispose();
			ClearOnScrollListeners();
			_recyclerViewScrollListener = null;
		}
	}
}
