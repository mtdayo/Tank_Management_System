using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TankManager : MonoBehaviour
{
    public enum TankState { RUN, STOP, ERROR }

    [System.Serializable]
    public class Tank
    {
        public string name;
        public float meter = 0f;
        public float fillInterval = 1f;
        public float fillAmount = 1f;
        public float repairTime = 10f;
        public TankState state = TankState.RUN;

        public TMP_Text nameText;           // タンク名表示
        public Slider meterSlider;          // メーター
        public TMP_Dropdown stateDropdown;  // 稼働/停止/故障（A/Bのみ）
        public Button resetButton;          // リセット（A/Bのみ）
        public Button repairButton;         // 修理ボタン

        [HideInInspector] public bool canControl = true;
        [HideInInspector] public bool isRepairing = false;
        [HideInInspector] public float timer = 0f;
    }

    public Tank tankA;
    public Tank tankB;
    public Tank tankC;

    private Tank currentRepairingTank = null;

    void Start()
    {
        // 名前表示
        if (tankA.nameText != null) tankA.nameText.text = tankA.name;
        if (tankB.nameText != null) tankB.nameText.text = tankB.name;
        if (tankC.nameText != null) tankC.nameText.text = tankC.name;

        // ボタン登録
        if (tankA.resetButton != null) tankA.resetButton.onClick.AddListener(() => ResetTank(tankA));
        if (tankB.resetButton != null) tankB.resetButton.onClick.AddListener(() => ResetTank(tankB));

        if (tankA.repairButton != null) tankA.repairButton.onClick.AddListener(() => StartRepair(tankA));
        if (tankB.repairButton != null) tankB.repairButton.onClick.AddListener(() => StartRepair(tankB));
        if (tankC.repairButton != null) tankC.repairButton.onClick.AddListener(() => StartRepair(tankC));

        // Dropdown登録
        if (tankA.stateDropdown != null) tankA.stateDropdown.onValueChanged.AddListener((int value) => OnDropdownChange(tankA, value));
        if (tankB.stateDropdown != null) tankB.stateDropdown.onValueChanged.AddListener((int value) => OnDropdownChange(tankB, value));
    }

    void Update()
    {
        // TankC故障確率0.1%
        if (tankC.state == TankState.RUN && Random.value < 0.001f)
        {
            Debug.LogWarning("Tank C has failed!");
            tankC.state = TankState.ERROR;
            tankA.state = TankState.STOP;
            tankB.state = TankState.STOP;
        }

        // メーター更新
        UpdateTank(tankA);
        UpdateTank(tankB);
        UpdateTank(tankC);

        // 相互稼働ルール
        CheckTankRules();

        // 修理ボタン/Dropdown更新
        UpdateUIInteractables();
    }

    void UpdateTank(Tank tank)
    {
        if (tank.state != TankState.RUN) return;

        tank.timer += Time.deltaTime;
        if (tank.timer >= tank.fillInterval)
        {
            tank.meter += tank.fillAmount;
            tank.timer = 0f;

            // 70%警告
            if (tank.meter >= 70f && tank.meter < 100f)
                Debug.LogWarning($"{tank.name} reached 70%!");

            // 100%処理
            if (tank.meter >= 100f)
            {
                if (tank != tankC)
                {
                    Debug.LogError($"{tank.name} reached 100%!");
                    tank.state = TankState.ERROR;
                    tank.canControl = false;
                }
                else
                {
                    // TankC 100% → A/Bはリセット（故障タンク除く）
                    if (tankA.state != TankState.ERROR) tankA.meter = 0;
                    if (tankB.state != TankState.ERROR) tankB.meter = 0;
                    tankC.meter = 0;
                    Debug.Log("Tank C reached 100%! All meters reset!");
                }
            }

            // Slider更新
            if (tank.meterSlider != null)
                tank.meterSlider.value = Mathf.Min(tank.meter, 100f);
        }
    }

    void ResetTank(Tank tank)
    {
        if (tank.state != TankState.ERROR)
            tank.meter = 0;
    }

    void OnDropdownChange(Tank tank, int value)
    {
        if (!tank.canControl) return;
        tank.state = (TankState)value;

        if (tank == tankA && (tank.state == TankState.STOP || tank.state == TankState.ERROR))
            if (tankB.state != TankState.ERROR) tankB.state = TankState.RUN;

        if (tank == tankB && (tank.state == TankState.STOP || tank.state == TankState.ERROR))
            if (tankA.state != TankState.ERROR) tankA.state = TankState.RUN;
    }

    void CheckTankRules()
    {
        if (tankC.state == TankState.ERROR)
        {
            if (tankA.state != TankState.ERROR) tankA.state = TankState.STOP;
            if (tankB.state != TankState.ERROR) tankB.state = TankState.STOP;
        }
    }

    void UpdateUIInteractables()
    {
        // A/BのDropdown操作
        if (tankA.stateDropdown != null)
            tankA.stateDropdown.interactable = tankA.canControl;
        if (tankB.stateDropdown != null)
            tankB.stateDropdown.interactable = tankB.canControl;

        // 修理ボタンは故障中かつ修理中なしで押せる
        if (tankA.repairButton != null)
            tankA.repairButton.interactable = tankA.state == TankState.ERROR && currentRepairingTank == null;
        if (tankB.repairButton != null)
            tankB.repairButton.interactable = tankB.state == TankState.ERROR && currentRepairingTank == null;
        if (tankC.repairButton != null)
            tankC.repairButton.interactable = tankC.state == TankState.ERROR && currentRepairingTank == null;
    }

    void StartRepair(Tank tank)
    {
        if (currentRepairingTank != null || tank.state != TankState.ERROR) return;
        StartCoroutine(RepairCoroutine(tank));
    }

    IEnumerator RepairCoroutine(Tank tank)
    {
        currentRepairingTank = tank;
        tank.isRepairing = true;
        Debug.Log($"Repairing {tank.name} for {tank.repairTime} seconds...");
        yield return new WaitForSeconds(tank.repairTime);

        // 修理完了後の状態
        if (tank == tankC)
        {
            tank.state = TankState.RUN;  // TankCは自動で稼働
        }
        else
        {
            tank.state = TankState.STOP; // TankA/Bは手動で稼働
        }

        tank.canControl = true;
        tank.isRepairing = false;
        currentRepairingTank = null;

        Debug.Log($"{tank.name} repaired!");
    }
}
