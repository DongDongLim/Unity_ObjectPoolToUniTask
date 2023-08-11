using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ObjectPoolSample
{

    public class SampleObject : MonoBehaviour
    {
        private Rigidbody2D _rigidbody2D;
        private SpriteRenderer _renderer;        

        private void Awake()
        {
            _rigidbody2D = GetComponent<Rigidbody2D>();
            _renderer = GetComponent<SpriteRenderer>();
        }

        private void OnEnable()
        {
            SetRandomScale();
            SetEnablePosition();
            SetRandomColor();
            SoarObject();
        }

        private void SetRandomScale()
        {
            transform.localScale = Random.insideUnitCircle;
        }

        private void SetEnablePosition()
        {
            transform.position = transform.parent.position;
        }

        private void SetRandomColor()
        {
            _renderer.color = new Color(Random.Range(0.1f, 1f), Random.Range(0.1f, 1f), Random.Range(0.1f, 1f));
        }

        private void SoarObject()
        {
            Vector2 randomForce = GetRandomForce();

            _rigidbody2D.velocity = Vector2.zero;
            _rigidbody2D.AddForceAtPosition(randomForce, transform.position, ForceMode2D.Impulse); 
        }

        private Vector2 GetRandomForce()
        {
            return (Vector2.up * Random.Range(10, 21)) + (Vector2.right * Random.Range(-5, 6));
        }
    }
}