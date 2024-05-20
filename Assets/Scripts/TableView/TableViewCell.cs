using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using System;
using System.Collections.Generic;

namespace qinzh
{
    [Serializable]
    public class TableViewCellClickEvent : UnityEvent<int>
    {

    }
    public abstract class TableViewCell : MonoBehaviour, IPointerClickHandler
    {
        //cell在tableview中的位置索引
        private int _idx = -1;
        public TableViewCellClickEvent ClickEvent;

        public int Index {
            get {
                return _idx;
            }
        }

        public void SetIndex(int idx) {
            _idx = idx;

            this.UpdateDisplay();
        }

        public void ResetCell()
        {
            _idx = -1;
            this.gameObject.SetActive(false);
        }

        public abstract void UpdateDisplay();

        public void OnPointerClick(PointerEventData eventData)
        {
            if (ClickEvent == null) return;

            ClickEvent.Invoke(Index);
        }
    }


    public class TableViewCellComparer : IComparer<TableViewCell>
    {
        public int Compare(TableViewCell x, TableViewCell y)
        {
            return x.Index.CompareTo(y.Index); 
        }
    }
}