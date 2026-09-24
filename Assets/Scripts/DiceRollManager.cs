using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class DiceRollManager : MonoBehaviour
{
    [Header("Dice Settings:")]
    [SerializeField] private DieRoller[] dice;          // all 5 slots, index 0-4
    [SerializeField] private int[] shuffleCounts = { 8, 12, 16, 20, 24 }; // die i shuffles this many times

    [Header("Timing:")]
    [SerializeField] private float raiseDelay = 0.1f;    // stagger between dice lifting up

    [Header("Add Die:")]
    [SerializeField] private GameObject addDieButton;    // Add Die button (starts inactive)

    [Header("Score Display:")]
    [SerializeField] private TextMeshProUGUI scoreText;  // TOTAL display
    [SerializeField] private TextMeshProUGUI multiplierText; // multiplier title + value
    [SerializeField] private Color scoreColor = Color.white;
    [SerializeField] private Color multiplierTitleColor = Color.yellow;

    private int activeDice = 0;   // how many dice currently in play (starts at 1)
    private bool awaitingClicks = false;

    private void Start()
    {
        // Start with only one die; the rest are hidden and inactive
        for (int i = 1; i < dice.Length; i++)
        {
            dice[i].gameObject.SetActive(false);
        }
        activeDice = 1;
    }

    // Roll button listener
    public void RollAllDice()
    {
        StartCoroutine(RollSequence());
    }

    private IEnumerator RollSequence()
    {
        awaitingClicks = false;

        // 1. All dice rise up together (staggered slightly so it reads as a group)
        for (int i = 0; i < activeDice; i++)
        {
            dice[i].ResetLayers();
            dice[i].RaiseUp();
            yield return new WaitForSeconds(raiseDelay);
        }
        yield return new WaitForSeconds(0.3f); // let the rise finish

        // 2. All dice shuffle at the same time, different counts so they drop one by one
        int done = 0;
        for (int i = 0; i < activeDice; i++)
        {
            DieRoller die = dice[i];
            die.OnRollEnd += OnDieFinished;
            die.BeginShuffle(shuffleCounts[i]);
        }

        void OnDieFinished(DieRoller d)
        {
            d.OnRollEnd -= OnDieFinished;
            done++;
        }

        // wait until every die has finished shuffling and dropped
        yield return new WaitUntil(() => done >= activeDice);

        // 3. Report results
        ReportResults();

        // 4. Enable locking for this turn
        awaitingClicks = true;
        for (int i = 0; i < activeDice; i++)
        {
            dice[i].CanLock = true;
        }
    }

    private void ReportResults()
    {
        List<int> values = new List<int>();
        int total = 0;

        for (int i = 0; i < activeDice; i++)
        {
            int v = dice[i].FaceValue;
            values.Add(v);
            total += v;
            Debug.Log($"Die {i + 1}: {v}");
        }

        (string title, float multiplier) = EvaluateHand(values);

        Debug.Log($"TOTAL: {total}");
        Debug.Log($"{title} x{multiplier}");

        // In-game display
        if (scoreText != null)
        {
            scoreText.text = $"TOTAL: {total}";
            scoreText.color = scoreColor;
        }
        if (multiplierText != null)
        {
            multiplierText.text = $"{title}  x{multiplier}";
            multiplierText.color = multiplierTitleColor;
        }
    }

    private static (string, float) EvaluateHand(List<int> v)
    {
        // count how many times each face appears
        int[] counts = new int[7];
        foreach (int x in v) counts[x]++;

        bool hasPair = false, hasTwoPair = false, hasThree = false, hasFour = false, hasFive = false;
        foreach (int c in counts)
        {
            if (c == 2) { if (hasPair) hasTwoPair = true; else hasPair = true; }
            if (c == 3) hasThree = true;
            if (c == 4) hasFour = true;
            if (c == 5) hasFive = true;
        }

        if (hasFive) return ("Five of a kind", 10f);
        if (hasFour) return ("Four of a kind", 5f);
        if (hasThree && hasPair) return ("Full House", 3.5f);
        if (counts[1] >= 1 && counts[2] >= 1 && counts[3] >= 1 && counts[4] >= 1 && counts[5] >= 1 && counts[6] >= 1 && v.Count == 5)
            return ("Straight", 3f);
        if (hasThree) return ("Three of a kind", 2.5f);
        if (hasTwoPair) return ("Two pairs", 2f);
        if (hasPair) return ("One pair", 1.5f);
        return ("High Roller", 1f);
    }

    // Add Die button listener: adds the next slot, max 5 total
    public void AddDie()
    {
        if (activeDice >= dice.Length) return;

        dice[activeDice].gameObject.SetActive(true);
        activeDice++;

        if (activeDice >= dice.Length)
        {
            if (addDieButton != null) addDieButton.SetActive(false); // fade the whole button out
        }
    }
}
