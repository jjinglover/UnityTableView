using System;

namespace qinzh
{
    public interface ITableViewDataSource
    {
        int NumberOfCellsInTableView(TableView tableView);
        float TableCellSizeForIndex(TableView tableView, int index);
        TableViewCell TableCellAtIndex(TableView tableView, int index);
    }

    public interface ITableViewDelegate
    {
        void TableCellClicked(TableView tableView, int index);
    }

    public interface ITableView
    {
        ITableViewDataSource DataSource { get; set; }
        ITableViewDelegate Delegate { get; set; }
        void ReloadData();
        TableViewCell ReusableCell();
        void setInnerPositionByIndex(int index);
    }
}