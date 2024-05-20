
using UnityEngine;

namespace qinzh
{
    public class Test : MonoBehaviour, ITableViewDataSource, ITableViewDelegate
    {
        public TableView tableView;
        public GameObject hCell = null;
        public GameObject vCell = null;
        void Start()
        {
            tableView.Delegate = this;
            tableView.DataSource = this;
            tableView.ReloadData();
            tableView.setInnerPositionByIndex(12);
        }

        public int NumberOfCellsInTableView(TableView tableView)
        {
            return 20;
        }

        public float TableCellSizeForIndex(TableView tableView, int index)
        {
            return 90;
        }

        public TableViewCell TableCellAtIndex(TableView tableView, int index)
        {
            TableViewCell cell = tableView.ReusableCell();
            if (cell == null)
            {
                GameObject obj = Instantiate(tableView.IsHorizontal ? hCell : vCell, tableView.InnerContainerContent().transform);
                cell = obj.GetComponent<TableViewCell>();
            }
            cell.name = "Cell " + index;
            return cell;
        }

        public void TableCellClicked(TableView tableView, int index)
        {
            Debug.Log("TableViewDidSelectCellForRow : " + index);
        }
    }
}