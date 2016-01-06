using System.Drawing;
using System.Windows.Automation;
using UiClickTestDSL.AutomationCode;

namespace UiClickTestDSL.DslObjects {
    public class ListUiItem {
        private readonly AutomationElement _ae;

        public ListUiItem(AutomationElement ae) {
            _ae = ae;
        }

        public string Name {
            get { return _ae.Current.Name; }
        }

        public Point ClickablePoint {
            get { return _ae.GetClickablePoint().Convert(); }
        }

        public void SetFocus() {
            _ae.SetFocus();
        }
    }
}
