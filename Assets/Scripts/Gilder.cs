using System;
using UnityEngine;


[RequireComponent (typeof(Rigidbody))]
public class Gilder : MonoBehaviour
{
    [SerializeField] private Transform _wingCP;

    [Header("Плотность воздуха")]
    [SerializeField] private float _airDensity = 1.225f;

    [Header("Аэродиномические характеристики крыла")]
    [SerializeField] private float _wingArea = 1.5f;
    [SerializeField] private float _wingAspect = 8.0f;

    [SerializeField] private float _wingCDD = 0.02f;

    [SerializeField] private float _wingClaplha = 5.5f;

    private Rigidbody _rigidbody;


    private Vector3 _vPoint;
    private Vector3 _worldVelocity;
    private float _speadMS;
    private float _alphaRad;

    private float _cl, _cd, _qDyn, _lMag, _dMag, _qlidek;
    private bool IsGround;
    private float _startPosition;
    
    private JetEngine _jetEngine;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
        
        if (_jetEngine == null)
        {
            _jetEngine = GetComponent<JetEngine>();
        }
    }


    private void FixedUpdate()
    {
        // скорость в точке крыла

        if (transform.position.y - 0.5f > _startPosition && IsGround)
        {
            //_wingCP.localEulerAngles = new Vector3(0, 180, 0);
        }
        
        _vPoint = _rigidbody.GetPointVelocity(_wingCP.position);
        _speadMS = _vPoint.magnitude;

        Vector3 flowDir = (-_vPoint).normalized;
        Vector3 xChord = _wingCP.forward;
        Vector3 zUP = _wingCP.up;
        Vector3 ySpan = _wingCP.right;


        float flowX = Vector3.Dot(lhs:flowDir, rhs:xChord);
        float flowZ = Vector3.Dot(lhs:flowDir, rhs:zUP);
        _alphaRad = Mathf.Atan2(y: flowZ, flowX);

        _cl = _wingClaplha * _alphaRad;
        _cd = _wingCDD + _cl * _cl / (Mathf.PI*_wingAspect * 0.85f);


        _qDyn = 0.5f * _airDensity * _speadMS * _speadMS;
        _lMag = _qDyn * _wingArea * _cl;
        _dMag = _qDyn * _wingArea * _cd;


        Vector3 Ddir = -flowDir;


        Vector3 liftDir = Vector3.Cross(lhs: flowDir, rhs:ySpan);
        liftDir.Normalize();
        

        Vector3 L = _lMag * liftDir;
        Vector3 D = _dMag * Ddir;


        _rigidbody.AddForceAtPosition(L + D, _wingCP.position, ForceMode.Force);

        // _worldVelocity = _rigidbody.linearVelocity;
        //_speadMS = _worldVelocity.magnitude;

    }

    private void StepOne()
    {
        Vector3 xChord = _wingCP.forward;//вдоль хорды
        Vector3 zUP = _wingCP.up;// нормаль к поверхности

        Vector3 flowDir = _speadMS > 0 ? _worldVelocity.normalized : _wingCP.forward;


        float flowX = Vector3.Dot(lhs: flowDir, rhs: xChord);
        float flowZ = Vector3.Dot(lhs: flowDir, rhs: zUP);

        _alphaRad = Mathf.Atan2(y: flowZ, flowX);
    }

    private void OnCollisionEnter(Collision other)
    {
        if (other.gameObject.CompareTag("Ground"))
        {
            _startPosition = transform.position.y;
            IsGround = true;
        }
    }

    private void OnGUI()
    {
        // Задаем область для вывода телеметрии в левом верхнем углу
        GUI.Box(new Rect(5, 5, 300, 450), "Телеметрия");
        GUILayout.BeginArea(new Rect(10, 25, 290, 420));
    
        GUI.color = Color.black;

        // --- Общие параметры ---
        GUILayout.Label("--- ОБЩЕЕ ---");
        GUILayout.Label(text: $"Скорость: {_speadMS:F1} м/с ({(int)(_speadMS * 3.6f)} км/ч)");
        GUILayout.Label(text: $"Высота: {transform.position.y:F1} м");
        GUILayout.Label(text: $"Вертикальная скорость: {_rigidbody.linearVelocity.y:F1} м/с");
    
        GUILayout.Space(10);

        // --- Аэродинамика ---
        GUILayout.Label("--- АЭРОДИНАМИКА ---");
        GUILayout.Label(text: $"Угол атаки: {_alphaRad * Mathf.Rad2Deg:F1}°"); 
        GUILayout.Label(text: $"Коэф. подъемной силы (Cl): {_cl:F2}");
        GUILayout.Label(text: $"Коэф. сопротивления (Cd): {_cd:F3}");
        GUILayout.Label(text: $"Аэродинамическое качество (К): {_qlidek:F1}");
        GUILayout.Label(text: $"Подъемная сила: {(int)_lMag} Н");
        GUILayout.Label(text: $"Сила сопротивления: {(int)_dMag} Н");
        GUILayout.Label(text: $"Динамический напор: {(int)_qDyn} Па");
    
        GUILayout.Space(10);
        
        if (_jetEngine != null)
        {
            GUILayout.Label("--- ДВИГАТЕЛЬ ---");
            GUILayout.Label(text: $"Тяга: {_jetEngine._throttle01:P0}"); // P0 - формат в процентах
            GUILayout.Label(text: $"Форсаж: {(_jetEngine._afterBurner ? "ВКЛ" : "ВЫКЛ")}");
            GUILayout.Label(text: $"Сила тяги: {(int)_jetEngine._lastAppliedThrust} Н");
        }

        GUILayout.EndArea();
    }

}