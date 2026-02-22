using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Events;

[Serializable]
public class SpecificStat
{
    public Dictionary<int, List<float>> values = new();
    public Action<float> OnChangeValue;

    public SpecificStat()
    {
        for (int i = 0; i < 6; i++)
        {
            values[i] = new List<float>() { 0, 0, 0, 0 };
        }
    }
    public SpecificStat(SpecificStat other)
    {
        for (int i = 0; i < 6; i++)
        {
            values[i] = new List<float>(other.values[i]);
        }
    }
    public void ResetValue()
    {
        for (int i = 0; i < 6; i++)
        {
            values[i] = new List<float>() { 0, 0, 0, 0 };
        }
    }
    public void AdjustValue(int slot, int index, float val)
    {
        values[slot][index] += val;
        OnChangeValue?.Invoke(Calculate());
    }
    public void SetValue(int slot, int index, float val)
    {
        //Debug.Log($"{slot} 슬롯에서 {index} 번째줄로 {val}만큼 증가");
        values[slot][index] = val;
        OnChangeValue?.Invoke(Calculate());
    }
    public float GetValue()
    {
        float ret = Calculate();
        return ret;
    }
    private float Calculate()
    {
        float sum = 0;

        foreach (List<float> value in values.Values.ToList())
        {
            foreach (float val in value)
            {
                sum += val;
            }
        }

        return sum;
    }
}
