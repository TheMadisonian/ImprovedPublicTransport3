using ColossalFramework.UI;
using ICities;
using ImprovedPublicTransport.Query;
using JetBrains.Annotations;
using UnityEngine;

namespace AutoLineColor
{
    [UsedImplicitly]
    public class UIExtender : LoadingExtensionBase
    {
        private LoadMode _mode;

        private UIButton _refreshBtn;
        private MouseEventHandler _refreshBtnClick;

        private static bool ActiveInMode(LoadMode mode)
        {
            // ReSharper disable once SwitchStatementMissingSomeCases
            switch (mode)
            {
                case LoadMode.NewGame:
                case LoadMode.NewGameFromScenario:
                case LoadMode.LoadGame:
                    return true;

                default:
                    return false;
            }
        }

        public override void OnLevelLoaded(LoadMode mode)
        {
            this._mode = mode;

            if (ActiveInMode(mode))
                AttachUI();
        }

        public override void OnLevelUnloading()
        {
            if (ActiveInMode(_mode))
                DetachUI();
        }

        private void AttachUI()
        {
            //Console.Instance.Message("Attaching UI");

            var ptwip = GameObject.Find("UIView").transform.GetComponentInChildren<PublicTransportWorldInfoPanel>();
            var ptwipUiPanel = ptwip.gameObject.GetComponent<UIPanel>();
            var linesOverview = (UIButton)ptwipUiPanel.Find("LinesOverview");
            var buttonPanel = (UIPanel)linesOverview.parent;

            _refreshBtn = buttonPanel.Find<UIButton>("RefreshNameAndColor");

            if (_refreshBtn != null)
                buttonPanel.RemoveUIComponent(_refreshBtn);

            _refreshBtn = buttonPanel.AddUIComponent<UIButton>();
            _refreshBtn.name = "RefreshNameAndColor";

            _refreshBtn.text = "Refresh Name/Color";
            _refreshBtn.tooltip = "Reassign the line name and color according to current AutoLineColor Redux settings.";
            _refreshBtn.textScale = .75f;
            _refreshBtn.font = linesOverview.font;
            _refreshBtn.hoveredTextColor = new Color32(7, 132, 255, 255);
            _refreshBtn.textPadding = new RectOffset(10, 10, 4, 2);
            _refreshBtn.autoSize = true;
            _refreshBtn.anchor = UIAnchorStyle.Left | UIAnchorStyle.Right | UIAnchorStyle.CenterVertical;
            _refreshBtn.normalBgSprite = "ButtonMenu";
            _refreshBtn.hoveredBgSprite = "ButtonMenuHovered";
            _refreshBtn.disabledBgSprite = "ButtonMenuDisabled";
            //refreshBtn.focusedBgSprite = "ButtonMenuFocused";
            _refreshBtn.pressedBgSprite = "ButtonMenuPressed";

            _refreshBtn.SendToBack();
            buttonPanel.ResetLayout();

            _refreshBtnClick = (x, y) =>
            {
                ushort lineId = WorldInfoCurrentLineIDQuery.Query(out _);
                if (lineId == 0) return;

                ColorMonitor.Instance?.ForceRefreshLine(lineId);
            };

            _refreshBtn.eventClick += _refreshBtnClick;

            //Console.Instance.Message("Attached UI");
        }

        private void DetachUI()
        {
            //Console.Instance.Message("Detaching UI");

            // ReSharper disable once InvertIf
            if (_refreshBtn != null)
            {
                _refreshBtn.eventClick -= _refreshBtnClick;
                _refreshBtnClick = null;

                _refreshBtn.parent.RemoveUIComponent(_refreshBtn);
                Object.Destroy(_refreshBtn.gameObject);
                _refreshBtn = null;
            }

            //Console.Instance.Message("Detached UI");
        }
    }
}
