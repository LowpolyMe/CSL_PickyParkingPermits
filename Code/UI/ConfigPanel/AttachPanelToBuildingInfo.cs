using UnityEngine;
using ColossalFramework.UI;
using PickyParking.Logging;
using PickyParking.ModLifecycle;
using PickyParking.ModEntry;
using PickyParking.Features.ParkingLotPrefabs;

namespace PickyParking.UI
{
    public sealed class AttachPanelToBuildingInfo : MonoBehaviour
    {
        private const string CityServicePanelName = "CityServiceWorldInfoPanel";
        private const string CityServicePanelLibraryName = "(Library) CityServiceWorldInfoPanel";
        private const string WrapperContainerPath = "Wrapper";
        private const float InjectionRetrySeconds = 1f;
        private const float WrapperPadding = 25f;

        private ParkingRulesConfigPanel _panel;
        private ParkingPrefabSupportPanel _supportPanel;
        private ushort _lastSelectedBuildingId;
        private bool _lastPrefabSupported;
        private float _nextInjectionAttemptTime;
        private UIComponent _hostPanel;
        private UIComponent _wrapperContainer;
        private UIComponent _paddingSpacer;
        private bool _isVisibilitySubscribed;
        private bool _hasLoggedViewDump;

        public void Start()
        {
            TryInject();
        }

        public void Update()
        {
            var runtime = ModRuntime.Current;
            if (runtime == null)
            {
                UpdatePanels(false);
                return;
            }

            if (_panel == null || _supportPanel == null)
            {
                if (Time.unscaledTime < _nextInjectionAttemptTime)
                    return;

                _nextInjectionAttemptTime = Time.unscaledTime + InjectionRetrySeconds;
                TryInject();
                if (_panel == null && _supportPanel == null)
                    return;
            }

            if (!runtime.FeatureGate.IsActive)
            {
                UpdatePanels(false);
                return;
            }

            if (!runtime.GameAccess.TryGetSelectedBuilding(out ushort buildingId, out BuildingInfo info))
            {
                _lastSelectedBuildingId = 0;
                UpdatePanels(false);
                return;
            }
            
            if (!HasParkingSpaces(runtime, buildingId))
            {
                _lastSelectedBuildingId = buildingId;
                _lastPrefabSupported = false;
                if (_panel != null)
                    _panel.SetPrefabSupported(false);
                SetMainVisible(false);
                SetSupportVisible(false);
                return;
            }

            bool prefabSupported = IsSupportedParkingLot(runtime, info);
            if (_panel != null)
                _panel.SetPrefabSupported(prefabSupported);

            bool supportChanged = _lastSelectedBuildingId == buildingId && _lastPrefabSupported != prefabSupported;
            BindPanelsForSelection(buildingId, info, prefabSupported, supportChanged);

            UpdatePanels(prefabSupported);
        }

        private static bool HasParkingSpaces(ModRuntime runtime, ushort buildingId)
        {
            int totalSpaces;
            bool hasParkingSpaces = runtime.GameAccess.TryGetParkingSpaceCount(buildingId, out totalSpaces) && totalSpaces > 0;
            return hasParkingSpaces;
        }

        private void TryInject()
        {
            if (_panel != null)
                return;

            GameObject panelObject = GameObject.Find(CityServicePanelLibraryName);
            if (panelObject == null)
                panelObject = GameObject.Find(CityServicePanelName);
            if (panelObject == null)
            {
                UIView view = UIView.GetAView();
                if (view != null)
                {
                    UIComponent found = view.FindUIComponent<UIComponent>(CityServicePanelLibraryName);
                    if (found == null)
                        found = view.FindUIComponent<UIComponent>(CityServicePanelName);
                    if (found != null)
                        panelObject = found.gameObject;

                    if (panelObject == null)
                    {
                        UIComponent prefixMatch = FindComponentByContains(view, CityServicePanelName);
                        if (prefixMatch != null)
                        {
                            panelObject = prefixMatch.gameObject;
                            if (Log.IsVerboseEnabled && Log.IsUiDebugEnabled)
                                Log.Info("[UI] CityServiceWorldInfoPanel found by prefix: " + prefixMatch.name);
                        }
                    }

                    if (panelObject == null && !_hasLoggedViewDump && Log.IsVerboseEnabled && Log.IsUiDebugEnabled)
                    {
                        _hasLoggedViewDump = true;
                        LogViewCandidates(view);
                    }
                }

                if (panelObject == null)
                {
                    if (Log.IsVerboseEnabled && Log.IsUiDebugEnabled)
                        Log.Info("[UI] CityServiceWorldInfoPanel not found yet.");
                    return;
                }
            }

            UIComponent root = panelObject.GetComponent<UIComponent>();
            if (root == null)
            {
                if (Log.IsVerboseEnabled && Log.IsUiDebugEnabled)
                    Log.Info("[UI] CityServiceWorldInfoPanel has no UIComponent.");
                return;
            }

            if (!_isVisibilitySubscribed)
            {
                _hostPanel = root;
                _hostPanel.eventVisibilityChanged += HandleHostVisibilityChanged;
                _isVisibilitySubscribed = true;
                if (Log.IsVerboseEnabled && Log.IsUiDebugEnabled)
                    Log.Info("[UI] Subscribed to CityServiceWorldInfoPanel visibility.");
            }

            Transform wrapperTransform = root.transform.Find(WrapperContainerPath);
            if (wrapperTransform == null)
            {
                if (Log.IsVerboseEnabled && Log.IsUiDebugEnabled)
                    Log.Info("[UI] Wrapper container path not found: " + WrapperContainerPath);
                return;
            }

            UIComponent wrapper = wrapperTransform.GetComponent<UIComponent>();
            if (wrapper == null)
            {
                if (Log.IsVerboseEnabled && Log.IsUiDebugEnabled)
                    Log.Info("[UI] Wrapper transform has no UIComponent.");
                return;
            }

            _wrapperContainer = wrapper;
            UIPanel wrapperPanel = _wrapperContainer as UIPanel;
            if (wrapperPanel != null && !wrapperPanel.autoFitChildrenVertically)
            {
                wrapperPanel.autoFitChildrenVertically = true;
                if (Log.IsVerboseEnabled && Log.IsUiDebugEnabled)
                    Log.Info("[UI] Wrapper autoFitChildrenVertically enabled.");
            }
            EnsureWrapperPaddingSpacer();
            var existing = wrapper.GetComponentInChildren<ParkingRulesConfigPanel>();
            if (existing != null)
            {
                _panel = existing;
                if (Log.IsVerboseEnabled && Log.IsUiDebugEnabled)
                    Log.Info("[UI] Reusing existing ParkingRulesConfigPanel.");
            }
            else
            {
                _panel = wrapper.AddUIComponent<ParkingRulesConfigPanel>();
                _panel.relativePosition = Vector3.zero;
                _panel.size = wrapper.size;
                int targetIndex = Mathf.Min(3, wrapper.childCount - 1);
                _panel.zOrder = targetIndex;
                _panel.transform.SetSiblingIndex(targetIndex);
                if (Log.IsVerboseEnabled && Log.IsUiDebugEnabled)
                    Log.Info("[UI] Injected ParkingRulesConfigPanel into Wrapper container.");
            }

            var supportExisting = wrapper.GetComponentInChildren<ParkingPrefabSupportPanel>();
            if (supportExisting != null)
            {
                _supportPanel = supportExisting;
                if (Log.IsVerboseEnabled && Log.IsUiDebugEnabled)
                    Log.Info("[UI] Reusing existing ParkingPrefabSupportPanel.");
                return;
            }

            _supportPanel = wrapper.AddUIComponent<ParkingPrefabSupportPanel>();
            _supportPanel.relativePosition = Vector3.zero;
            _supportPanel.size = wrapper.size;
            int supportIndex = Mathf.Min(3, wrapper.childCount - 1);
            _supportPanel.zOrder = supportIndex;
            _supportPanel.transform.SetSiblingIndex(supportIndex);
            if (Log.IsVerboseEnabled && Log.IsUiDebugEnabled)
                Log.Info("[UI] Injected ParkingPrefabSupportPanel into Wrapper container.");
        }

        public void OnDestroy()
        {
            if (_panel == null && _supportPanel == null)
            {
                UnsubscribeVisibilityHandler();
                return;
            }

            if (_panel != null)
            {
                _panel.DiscardUnappliedChangesIfAny();
                _panel.ClearPreview();
                _panel.RequestPendingReevaluationIfAny();
                UnityEngine.Object.Destroy(_panel.gameObject);
                _panel = null;
            }
            if (_supportPanel != null)
            {
                UnityEngine.Object.Destroy(_supportPanel.gameObject);
                _supportPanel = null;
            }
            _lastSelectedBuildingId = 0;
            _nextInjectionAttemptTime = 0f;
            _hasLoggedViewDump = false;
            UnsubscribeVisibilityHandler();
        }

        private void UpdatePanels(bool shouldShowMainPanel)
        {
            if (shouldShowMainPanel)
            {
                SetSupportVisible(false);
                SetMainVisible(true);
            }
            else
            {
                SetMainVisible(false);
                SetSupportVisible(true);
            }

            // ResizeWrapperToFitChildren();
        }

        private void SetMainVisible(bool visible)
        {
            if (_panel == null)
                return;
            if (_panel.isVisible == visible)
                return;

            if (!visible)
            {
                _panel.DiscardUnappliedChangesIfAny();
                _panel.ClearPreview();
                _panel.RequestPendingReevaluationIfAny();
            }

            _panel.isVisible = visible;
        }

        private void SetSupportVisible(bool visible)
        {
            if (_supportPanel == null)
                return;
            if (_supportPanel.isVisible == visible)
                return;

            _supportPanel.isVisible = visible;
        }

        private void BindPanelsForSelection(ushort buildingId, BuildingInfo info, bool prefabSupported, bool supportChanged)
        {
            if (_lastSelectedBuildingId != buildingId)
            {
                _lastSelectedBuildingId = buildingId;
                _lastPrefabSupported = prefabSupported;
                if (_panel != null && prefabSupported)
                    _panel.Bind(buildingId);
                if (_supportPanel != null && !prefabSupported)
                    _supportPanel.Bind(buildingId, info);
                return;
            }

            if (supportChanged)
            {
                if (_panel != null && prefabSupported)
                    _panel.Bind(buildingId);
                if (_supportPanel != null && !prefabSupported)
                    _supportPanel.Bind(buildingId, info);
                _lastPrefabSupported = prefabSupported;
            }
        }

        private static bool IsSupportedParkingLot(ModRuntime runtime, BuildingInfo info)
        {
            if (runtime == null || info == null)
                return false;

            if (runtime.SupportedParkingLotRegistry == null)
                return false;

            var key = ParkingLotPrefabKeyFactory.CreateKey(info);
            return runtime.SupportedParkingLotRegistry.Contains(key);
        }

        private void HandleHostVisibilityChanged(UIComponent component, bool isVisible)
        {
            if (isVisible)
                return;

            UpdatePanels(false);
        }

        private void UnsubscribeVisibilityHandler()
        {
            if (!_isVisibilitySubscribed || _hostPanel == null)
                return;

            _hostPanel.eventVisibilityChanged -= HandleHostVisibilityChanged;
            _hostPanel = null;
            _isVisibilitySubscribed = false;
        }

        /*
        private void ResizeWrapperToFitChildren()
        {
            if (_wrapperContainer == null)
                return;

            float maxBottom = 0f;
            int childCount = _wrapperContainer.transform.childCount;
            for (int i = 0; i < childCount; i++)
            {
                Transform childTransform = _wrapperContainer.transform.GetChild(i);
                UIComponent child = childTransform.GetComponent<UIComponent>();
                if (child == null || !child.isVisible)
                    continue;

                float bottom = child.relativePosition.y + child.height;
                if (bottom > maxBottom)
                    maxBottom = bottom;
                if (Log.IsVerboseEnabled)
                    Log.Info("[UI] Wrapper child '" + child.name + "' y=" + child.relativePosition.y + " height=" + child.height + " bottom=" + bottom);

            }

            if (maxBottom <= 0f)
            {
                if (Log.IsVerboseEnabled)
                    Log.Info("[UI] Wrapper resize skipped. maxBottom=" + maxBottom + " childCount=" + childCount);
                return;
            }

            float paddedBottom = maxBottom + WrapperPadding;
            if (!Mathf.Approximately(_wrapperContainer.height, paddedBottom))
            {
                if (Log.IsVerboseEnabled)
                    Log.Info("[UI] Wrapper resize: maxBottom=" + maxBottom + " paddedBottom=" + paddedBottom + " prevHeight=" + _wrapperContainer.height);
                _wrapperContainer.height = paddedBottom;
            }
        }
        */

        private void EnsureWrapperPaddingSpacer()
        {
            if (_wrapperContainer == null)
                return;

            if (_paddingSpacer != null)
                return;

            UIPanel spacer = _wrapperContainer.AddUIComponent<UIPanel>();
            spacer.name = "PickyParkingWrapperPadding";
            spacer.width = _wrapperContainer.width;
            spacer.height = WrapperPadding;
            spacer.isVisible = true;
            spacer.autoLayout = false;
            spacer.backgroundSprite = string.Empty;
            spacer.zOrder = _wrapperContainer.childCount - 1;
            _paddingSpacer = spacer;

            if (Log.IsVerboseEnabled && Log.IsUiDebugEnabled)
                Log.Info("[UI] Added wrapper padding spacer.");
        }

        private static UIComponent FindComponentByContains(UIView view, string token)
        {
            if (view == null || string.IsNullOrEmpty(token))
                return null;

            UIComponent[] components = view.GetComponentsInChildren<UIComponent>(true);
            for (int i = 0; i < components.Length; i++)
            {
                UIComponent component = components[i];
                if (component != null && component.name != null
                    && component.name.IndexOf(token, System.StringComparison.Ordinal) >= 0)
                    return component;
            }

            return null;
        }

        private static void LogViewCandidates(UIView view)
        {
            UIComponent[] components = view.GetComponentsInChildren<UIComponent>(true);
            int logged = 0;
            for (int i = 0; i < components.Length; i++)
            {
                UIComponent component = components[i];
                if (component == null || string.IsNullOrEmpty(component.name))
                    continue;

                if (!component.name.Contains("WorldInfoPanel")
                    && !component.name.Contains("CityService"))
                    continue;

                string path = GetTransformPath(component.transform);
                if (Log.IsVerboseEnabled && Log.IsUiDebugEnabled)
                    Log.Info("[UI] Panel candidate: " + component.name + " Path=" + path);
                if (++logged >= 25)
                    break;
            }

            if (logged == 0)
            {
                if (Log.IsVerboseEnabled && Log.IsUiDebugEnabled)
                    Log.Info("[UI] No WorldInfoPanel candidates found in UIView.");
            }
        }

        private static string GetTransformPath(Transform transform)
        {
            if (transform == null)
                return "<null>";

            System.Text.StringBuilder sb = new System.Text.StringBuilder(128);
            Transform current = transform;
            while (current != null)
            {
                if (sb.Length == 0)
                    sb.Insert(0, current.name);
                else
                    sb.Insert(0, current.name + "/");

                current = current.parent;
            }

            return sb.ToString();
        }
    }
}
