using System.Drawing;
using System.Windows.Forms;

namespace Hue_travel.Theme
{
    /// <summary>
    /// A scrollable Panel that does not jump when a child takes focus. The stock AutoScroll panel
    /// scrolls the first focusable child into view the moment its form is shown, which cut the
    /// top of the route and guide panels.
    /// </summary>
    public class NoAutoScrollPanel : Panel
    {
        protected override Point ScrollToControl(Control activeControl)
        {
            return DisplayRectangle.Location;
        }
    }
}
