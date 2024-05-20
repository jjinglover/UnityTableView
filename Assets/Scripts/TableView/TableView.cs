using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace qinzh
{
    public class TableView : MonoBehaviour, ITableView
    {
        /*
            横向滚动时,cell的排列为left->right
            纵向滚动时,cell的排列为bottom->top
         */
        public ITableViewDataSource DataSource
        {
            get { return _dataSource; }
            set { _dataSource = value; }
        }

        public ITableViewDelegate Delegate
        {
            get { return _tableViewDelegate; }
            set { _tableViewDelegate = value; }
        }

        private readonly int INVALID_INDEX = -1;

        private ITableViewDataSource _dataSource = null;
        private ITableViewDelegate _tableViewDelegate = null;
        //Scrollview的内容承载容器
        private ScrollRect _innerContainerRect = null;
        private List<TableViewCell> _usingCellsList;
        private Stack<TableViewCell> _freedCellsStack;
        //cell在容器中的坐标信息
        private List<float> _cellsPositionsList;
        //显示中的cell占用的idx信息
        private List<int> _cellUsingIdxs;
        //是否刷新cell显示标识
        private bool _requiresRefresh = false;
        public bool IsHorizontal
        {
            get { return _innerContainerRect.horizontal; }
        }

        //可视区域大小
        private Rect TableViewSize
        {
            get
            {
                Rect rect = (this.transform as RectTransform).rect;
                return rect;
            }
        }

        private float InnerContainerSizeDelta
        {
            get
            {
                if (IsHorizontal)
                {
                    return InnerContainerContent().sizeDelta.x;
                }
                else
                {
                    return InnerContainerContent().sizeDelta.y;
                }
            }
            set
            {
                if (IsHorizontal)
                {
                    InnerContainerContent().sizeDelta = new Vector2(value, InnerContainerContent().sizeDelta.y);
                }
                else
                {
                    InnerContainerContent().sizeDelta = new Vector2(InnerContainerContent().sizeDelta.x, value);
                }
            }
        }

        public RectTransform InnerContainerContent()
        {
            return _innerContainerRect.content;
        }

        private float GetContentOffset()
        {
            float oft = 0;
            if (IsHorizontal)
            {
                oft = 0 - InnerContainerContent().transform.localPosition.x;
            }
            else
            {
                oft = InnerContainerContent().sizeDelta.y - InnerContainerContent().transform.localPosition.y;
            }
            return oft;
        }


        void Awake()
        {
            _innerContainerRect = gameObject.GetComponent<ScrollRect>();
            _usingCellsList = new List<TableViewCell>();
            _freedCellsStack = new Stack<TableViewCell>();
            _cellsPositionsList = new List<float>();
            _cellUsingIdxs = new List<int>();

            if (IsHorizontal)
            {
                _innerContainerRect.content.localPosition = new Vector2(0, -TableViewSize.height * 0.5f);
                _innerContainerRect.content.pivot = new Vector2(0, 0.5f);
            }
            else
            {
                _innerContainerRect.content.localPosition = new Vector2(0, 0);
                _innerContainerRect.content.pivot = new Vector2(0.5f, 1.0f);
            }
        }

        void LateUpdate()
        {
            if (_requiresRefresh)
            {
                _requiresRefresh = false;
                this.UpdateCellsShow();
            }
        }

        void OnEnable()
        {
            _innerContainerRect.onValueChanged.AddListener(ScrollViewValueChanged);
        }

        void OnDisable()
        {
            _innerContainerRect.onValueChanged.RemoveListener(ScrollViewValueChanged);
        }

        private void ScrollViewValueChanged(Vector2 newScrollValue)
        {
            //滚动时触发刷新操作
            _requiresRefresh = true;
        }

        /***************************************
                动态使用cell核心逻辑开始
         **************************************/

        //计算所有cell的坐标信息
        private void CaculateCellPosition()
        {
            int cellsCount = _dataSource.NumberOfCellsInTableView(this);
            if (cellsCount > 0)
            {
                _cellsPositionsList.Clear();

                float currentPos = 0;
                for (int i = 0; i < cellsCount; i++)
                {
                    _cellsPositionsList.Add(currentPos);
                    float rowSize = _dataSource.TableCellSizeForIndex(this, i);
                    currentPos += rowSize;
                }
                _cellsPositionsList.Add(currentPos);
            }
        }

        //更新内嵌容器的size
        private void UpdateContentSize()
        {
            int cellsCount = _dataSource.NumberOfCellsInTableView(this);
            if (cellsCount > 0)
            {
                float maxPosition = _cellsPositionsList[cellsCount];
                if (IsHorizontal)
                {
                    if (maxPosition > TableViewSize.width)
                    {
                        InnerContainerSizeDelta = maxPosition - TableViewSize.width;
                    }
                }
                else 
                {
                    InnerContainerSizeDelta = maxPosition;
                }
            }
        }

        //获取指定位置对应cell的索引
        private int GetIndexFromOffset(float offset)
        {
            int index = 0;
            int maxIdx = _dataSource.NumberOfCellsInTableView(this) - 1;
            index = this.CaculateIndexFromOffset(offset);
            if (index != INVALID_INDEX)
            {
                index = Math.Max(0, index);
                if (index > maxIdx)
                {
                    index = INVALID_INDEX;
                }
            }
            return index;
        }

        //计算指定位置对应cell的索引
        private int CaculateIndexFromOffset(float offset)
        {
            int low = 0;
            int high = _dataSource.NumberOfCellsInTableView(this) - 1;
            float search = offset;

            while (high >= low)
            {
                int index = low + (high - low) / 2;
                float cellStart = _cellsPositionsList[index];
                float cellEnd = _cellsPositionsList[index + 1];

                if (search >= cellStart && search <= cellEnd)
                {
                    return index;
                }
                else if (search < cellStart)
                {
                    high = index - 1;
                }
                else
                {
                    low = index + 1;
                }
            }

            if (low <= 0)
            {
                return 0;
            }
            return INVALID_INDEX;
        }

        //更新视野内cell的显示
        private void UpdateCellsShow()
        {
            int countOfItems = _dataSource.NumberOfCellsInTableView(this);
            if (0 == countOfItems)
            {
                return;
            }

            int startIdx = 0, endIdx = 0, idx = 0, maxIdx = 0;
            float offset = this.GetContentOffset();
            maxIdx = Mathf.Max(countOfItems - 1, 0);

            endIdx = this.GetIndexFromOffset(offset);
            if (endIdx == INVALID_INDEX)
            {
                endIdx = countOfItems - 1;
            }

            offset -= IsHorizontal ? -TableViewSize.width : this.TableViewSize.height;
            startIdx = this.GetIndexFromOffset(offset);
            if (startIdx == -1)
            {
                startIdx = countOfItems - 1;
            }

            if (IsHorizontal)
            {
                //横向与纵向相反
                int tmp = startIdx;
                startIdx = endIdx;
                endIdx = tmp;
            }

            //--
            _usingCellsList.Sort(new TableViewCellComparer());
            //--检测可回收的cell--BEGIN
            if (_usingCellsList.Count > 0)
            {
                var cell = _usingCellsList[0];
                idx = cell.Index;

                while (idx < startIdx)
                {
                    this.MoveCellOutOfSight(cell);
                    if (_usingCellsList.Count > 0)
                    {
                        cell = _usingCellsList[0];
                        idx = cell.Index;
                    }
                    else
                    {
                        break;
                    }
                }
            }
            if (_usingCellsList.Count > 0)
            {
                var cell = _usingCellsList[_usingCellsList.Count - 1];
                idx = cell.Index;

                while (idx <= maxIdx && idx > endIdx)
                {
                    this.MoveCellOutOfSight(cell);
                    if (_usingCellsList.Count > 0)
                    {
                        cell = _usingCellsList[_usingCellsList.Count - 1];
                        idx = cell.Index;
                    }
                    else
                    {
                        break;
                    }
                }
            }
            //--检测可回收的cell--END

            for (int i = startIdx; i <= endIdx; i++)
            {
                if (_cellUsingIdxs.Contains(i))
                {
                    continue;
                }
                this.UpdateCellByIndex(i);
            }
        }
        //更新指定cell的显示
        private void UpdateCellByIndex(int index)
        {
            TableViewCell cell = _dataSource.TableCellAtIndex(this, index);
            cell.SetIndex(index);
            cell.ClickEvent.RemoveListener(CellDidClick);
            cell.ClickEvent.AddListener(CellDidClick);
            if (cell.gameObject.activeSelf == false)
            {
                cell.gameObject.SetActive(true);
            }
            //--
            float cellSize = _dataSource.TableCellSizeForIndex(this, index);
            Vector2 pos = new Vector2();
            if (IsHorizontal)
            {
                pos.x = _cellsPositionsList[index] - InnerContainerSizeDelta * 0.5f - 0.5f * TableViewSize.width + cellSize * 0.5f;
                pos.y = 0;
            }
            else 
            {
                pos.x = 0;
                pos.y = _cellsPositionsList[index] - InnerContainerSizeDelta * 0.5f + cellSize * 0.5f;
            }
            cell.gameObject.GetComponent<RectTransform>().anchoredPosition = pos;
            //--
            _usingCellsList.Add(cell);
            _cellUsingIdxs.Add(cell.Index);
        }

        //回收视野外的cell
        private void MoveCellOutOfSight(TableViewCell cell)
        {
            _freedCellsStack.Push(cell);
            _usingCellsList.Remove(cell);
            _cellUsingIdxs.Remove(cell.Index);

            cell.ClickEvent.RemoveListener(CellDidClick);
            cell.ResetCell();
        }

        /***************************************
                动态使用cell核心逻辑结束
         **************************************/

        private void CellDidClick(int row)
        {
            if (_tableViewDelegate != null)
            {
                _tableViewDelegate.TableCellClicked(this, row);
            }
        }

        //刷新显示
        public void ReloadData()
        {
            foreach (TableViewCell cell in _usingCellsList)
            {
                cell.ResetCell();
                _freedCellsStack.Push(cell);
            }
            _usingCellsList.Clear();

            this.CaculateCellPosition();
            this.UpdateContentSize();
            this.UpdateCellsShow();
        }

        //获取可回收的cell
        public TableViewCell ReusableCell()
        {
            if (_freedCellsStack.Count > 0)
            {
                return _freedCellsStack.Pop();
            }
            return null;
        }

        //根据指定索引设置内嵌容器的位置，使之显示在视野范围
        public void setInnerPositionByIndex(int index)
        {
            int countOfItems = _dataSource.NumberOfCellsInTableView(this);
            var curLocalPos = InnerContainerContent().transform.localPosition;
            if (IsHorizontal)
            {
                float cellSize = _dataSource.TableCellSizeForIndex(this, index);
                float newP = TableViewSize.width - _cellsPositionsList[index] - cellSize;
                InnerContainerContent().transform.localPosition = new Vector2(newP, curLocalPos.y);
            }
            else 
            {
                int newIndex = countOfItems - index - 1;
                if (newIndex > 0 && newIndex < _cellsPositionsList.Count)
                {
                    float newP = _cellsPositionsList[newIndex];
                    InnerContainerContent().transform.localPosition = new Vector2(curLocalPos.x, newP);
                }
            }

            _requiresRefresh = true;
        }
    }
}