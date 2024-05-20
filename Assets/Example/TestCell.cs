
using UnityEngine;
using UnityEngine.UI;

namespace qinzh
{
    public class TestCell : TableViewCell
    {
        public Text txt;

        public override void UpdateDisplay()
        {
            txt.text = "Index " + Index;
        }
    }
}