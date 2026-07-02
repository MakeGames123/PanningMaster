using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class GameProgressManager : MonoBehaviour
{
    [SerializeField] Player player;
    [SerializeField] Machine machine;
    [SerializeField] Desk desk;
    [SerializeField] Prison prison;

    [Header("Camera Setting")]
    [SerializeField] CameraMove cameraMove;
    [SerializeField] Vector3 firstCameraMove;
    [SerializeField] Vector3 prisonCameraMove;

    [Header("Arrow Setting")]
    [SerializeField] ArrowHover hoverArrow;
    [SerializeField] NavigationArrow arrowNav;
    [SerializeField] Vector3 hoverArrowFirstPosition;
    [SerializeField] Vector3 hoverArrowSecondPosition;
    [SerializeField] Vector3 hoverArrowThirdPosition;
    [SerializeField] Vector3 hoverArrowFourthPosition;

    [Header("Input Slots")]
    [SerializeField] GameObject drillInput;
    [SerializeField] GameObject prisonInput;


    void Awake()
    {
        prison.onPrisonFull.AddListener(LastPhase);
        FirstMining();
    }

    private void FirstMining()
    {
        hoverArrow.gameObject.SetActive(true);
        hoverArrow.transform.position = hoverArrowFirstPosition;

        player.onMiningStart.AddListener(FirstMiningEnd);
    }

    private void FirstMiningEnd()
    {
        hoverArrow.gameObject.SetActive(false);

        player.onMiningStart.RemoveListener(FirstMiningEnd);

        FirstMachineDeliver();
    }

    private void FirstMachineDeliver()
    {
        arrowNav.gameObject.SetActive(true);
        arrowNav.SetTarget(new Vector3(hoverArrowSecondPosition.x, 0, hoverArrowSecondPosition.z));

        hoverArrow.gameObject.SetActive(true);
        hoverArrow.transform.position = hoverArrowSecondPosition;
        machine.OnIronCountChanged.AddListener(FirstMachineDeliverEnd);
    }
    private void FirstMachineDeliverEnd(int a, int b)
    {
        machine.OnIronCountChanged.RemoveListener(FirstMachineDeliverEnd);
        hoverArrow.gameObject.SetActive(false);
        arrowNav.gameObject.SetActive(false);

        FirstHandCuffClaim();
    }

    private void FirstHandCuffClaim()
    {
        arrowNav.gameObject.SetActive(true);
        arrowNav.SetTarget(new Vector3(hoverArrowThirdPosition.x, 0, hoverArrowThirdPosition.z));

        hoverArrow.gameObject.SetActive(true);
        hoverArrow.transform.position = hoverArrowThirdPosition;
        hoverArrow.StartHover();
        player.OnHandcuffCountChanged.AddListener(FirstHandCuffClaimEnd);
    }

    private void FirstHandCuffClaimEnd(int count)
    {
        arrowNav.gameObject.SetActive(false);
        hoverArrow.gameObject.SetActive(false);

        player.OnHandcuffCountChanged.RemoveListener(FirstHandCuffClaimEnd);

        FirstHandCuffDeliver();
    }

    private void FirstHandCuffDeliver()
    {
        hoverArrow.gameObject.SetActive(true);
        hoverArrow.transform.position = hoverArrowFourthPosition;
        hoverArrow.StartHover();

        desk.onHandCuffChanged.AddListener(FirstHandCuffDeliverEnd);
    }

    private void FirstHandCuffDeliverEnd(int count)
    {
        hoverArrow.gameObject.SetActive(false);

        desk.onHandCuffChanged.RemoveListener(FirstHandCuffDeliverEnd);

        player.OnMoneyCountChanged.AddListener(DrillUnlock);
    }

    private void DrillUnlock(int a)
    {
        player.OnMoneyCountChanged.RemoveListener(DrillUnlock);
        cameraMove.ShowTargetPosition(firstCameraMove);
        drillInput.SetActive(true);
    }
    private void LastPhase()
    {
        cameraMove.ShowTargetPosition(prisonCameraMove);
        prisonInput.SetActive(true);
    }
}
