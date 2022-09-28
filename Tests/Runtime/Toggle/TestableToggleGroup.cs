using UnityEngine.UI;

namespace ToggleTest
{
    class TestableToggleGroup : ToggleGroup
    {
        public bool ToggleListContains(Toggle toggle)
        {
            return m_Toggles.Contains(toggle);
        }
    }
}
