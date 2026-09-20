using System;
using System.Collections;
using Unity.VisualScripting;
using UnityEngine;

public class DieRoller : MonoBehaviour
{
    [Header("Animaion Settings:")]
    [SerializeField] private float rise = 0.5f;
    [SerializeField] private float animation = 0.25f;
    [SerializeField] private int shuffle = 10;
    [SerializeField] private float shuffleDelay = 0.2f;
    [SerializeField] private int dieFaces = 6;

    [Header("Layer Settings:")]
    [SerializeField] private float layerStep = 0.1f;

    private Transform[] move;
    private Vector3[] localCoordinate;
    private bool isRolling = false;
    private int topFace = 0;
    private float layerOffset = 0f;

    public event Action<int> OnRollEnd;

    private void Awake()
    {
        move = new Transform[dieFaces];
        localCoordinate = new Vector3[dieFaces];

        for (int i = 0; i < dieFaces; i++)
        {
            move[i] = transform.GetChild(i);
            localCoordinate[i] = move[i].localPosition;
        }
    }

    public void RollButtonPressed()
    {
        ResetLayers();
        RollDie();
    }

    public void RollDie()
    {
        if (!isRolling)
        {
            StartCoroutine(RollAnimation());
        }
    }

    // Initialize z coordinete
    public void ResetLayers()
    {
        layerOffset = 0f;

        for (int i = 0; i < dieFaces; i++)
        {
            move[i].localPosition = new Vector3(localCoordinate[i].x, localCoordinate[i].y, 0f);
        }
    }

    private IEnumerator RollAnimation()
    {
        isRolling = true;

        // Shuffling
        for (int i = 0; i < shuffle; i++)
        {
            RandomizeLayers();
            yield return new WaitForSeconds(shuffleDelay);
        }


        isRolling = false;
    }

    private void RandomizeLayers()
    {
        int randomIndex = UnityEngine.Random.Range(0, dieFaces);
        layerOffset += layerStep;

        move[randomIndex].localPosition = new Vector3(localCoordinate[randomIndex].x, localCoordinate[randomIndex].y, layerOffset);
        topFace = randomIndex;
    }

}
