using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;

public class TextAnimator : MonoBehaviour
{
    [SerializeField]
    private TextMeshPro text;

    private Vector3 _size;

    public void Init()
    {
        Transform myTransform = transform;
        _size = myTransform.lossyScale;
        myTransform.localScale = Vector3.zero;
    }
    
    public Sequence GetAnimation()
    {
        Sequence sq = DOTween.Sequence();
        sq.Append(transform.DOScale(_size, 0.4f).From(Vector3.zero).SetEase(Ease.OutSine));
        sq.Join(transform.DOShakeRotation(0.6f, 150f).SetEase(Ease.OutSine));
        return sq;
    }
}
