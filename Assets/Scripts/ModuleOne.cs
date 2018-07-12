using KKSpeech;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ModuleOne : MonoBehaviour {

    #region Private variables

    #region Speed settings
    private static float _ThrustersSpeed = 0.75f; //0.1f;
    private static float _YawSpeed = 2f; //0.25f; 
    private static float _PitchSpeed = 2f; //0.25f;
    private static float _ThrustPercentage = 1f;
    #endregion

    #region Game settings
    private static float _DockingTolerance = 0.35f;
    private static float _AsteroidRotateSpeed = 100f;
    private static float _AsteroidZoomSpeed = 10f;
    private static int _MovementDuration = 3;
    #endregion

    #region Status variables
    private static bool _DockedToMagellan;
    private static bool _DockedToStation;

    private static bool _EnginesEnabled;

    private static bool _ThrustersForwardEnabled;
    private static bool _ThrustersReverseEnabled;
    private static bool _YawLeftEnabled;
    private static bool _YawRightEnabled;
    private static bool _PitchUpEnabled;
    private static bool _PitchDownEnabled;

    private static string _StaticStatus = "";

    private float _NextActionTime = 0.0f;
    private float _Period = 0.5f;

    private Vector3 _PreviousPosition;
    #endregion

    #endregion

    #region Public GameObject properties
    public Text StatusText;
    public Text TargetText;
    public Text VelocityText;
    public Text ThrustUpgradeText;
    public GameObject BM;
    public GameObject BMTwo;
    public GameObject Spacecraft;
    public GameObject Asteroid;
    public GameObject ResetSwitch;

    public GameObject StationDockingMechanism;
    public GameObject MagellanDockingMechanism;
    public GameObject SpacecraftDockingMechanism;
    #endregion

    #region Audio sources
    public GvrAudioSource DockedAudioSource;
    public GvrAudioSource ComputerNotificationAudioSource;
    public GvrAudioSource ThrusterAudioSource;
    #endregion

    #region Initialization
    void Start()
    {
        _ThrustersForwardEnabled = false;
        _ThrustersReverseEnabled = false;
        _YawLeftEnabled = false;
        _YawRightEnabled = false;
        _PitchUpEnabled = false;
        _PitchDownEnabled = false;

        _DockedToMagellan = false;
        _DockedToStation = false;

        _EnginesEnabled = true;

        if (ResetSwitch != null)
            ResetSwitch.SetActive(false);

        if (StatusText != null)
        {
            StatusText.text = "IDLE";
            _StaticStatus = StatusText.text;
        }

        if (ThrustUpgradeText != null)
        {
            ThrustUpgradeText.text = ((int)((_ThrustPercentage - 1) * 100)).ToString() + "%";
        }

        if (SpeechRecognizer.ExistsOnDevice())
        {
            if (ComputerNotificationAudioSource != null)
                ComputerNotificationAudioSource.Play();

            SpeechRecognizerListener listener = GameObject.FindObjectOfType<SpeechRecognizerListener>();
            listener.onAuthorizationStatusFetched.AddListener(OnAuthorizationStatusFetched);
            listener.onAvailabilityChanged.AddListener(OnAvailabilityChange);
            listener.onErrorDuringRecording.AddListener(OnError);
            listener.onErrorOnStartRecording.AddListener(OnError);
            listener.onFinalResults.AddListener(OnFinalResult);
            listener.onPartialResults.AddListener(OnPartialResult);
            listener.onEndOfSpeech.AddListener(OnEndOfSpeech);
            SpeechRecognizer.SetDetectionLanguage("en-US");
            SpeechRecognizer.RequestAccess();
        }
        else
        {
            if (StatusText != null)
            {
                StatusText.text = "IDLE - VOICE COMMANDS NOT SUPPORTED";
                _StaticStatus = StatusText.text;
            }
        }
    }
    #endregion

    #region Status display
    private void OnAvailabilityChange(bool available)
    {
        if (!available)
        {
            if (StatusText != null)
            {
                StatusText.text = "IDLE - VOICE COMMANDS NOT AVAILABLE";
                _StaticStatus = StatusText.text;
            }
        }
        else
        {
            if (StatusText != null)
            { 
                StatusText.text = "IDLE - VOICE COMMANDS AVAILABLE";
                _StaticStatus = StatusText.text;
            }
        }
    }

    private void OnAuthorizationStatusFetched(AuthorizationStatus status)
    {
        switch (status)
        {
            case AuthorizationStatus.Authorized:
                if (StatusText != null)
                {
                    StatusText.text = "IDLE - VOICE COMMANDS AUTHORIZED";
                    _StaticStatus = StatusText.text;
                }

                break;
            default:
                if (StatusText != null)
                {
                    StatusText.text = "IDLE - VOICE COMMANDS NOT AUTHORIZED";
                    _StaticStatus = StatusText.text;
                }
                break;
        }
    }
    #endregion

    #region Result functions
    private void OnFinalResult(string result)
    {
        if (StatusText != null)
        {
            StatusText.text = result;
            _StaticStatus = StatusText.text;

            ProcessResult(result);
        }
    }

    private void OnPartialResult(string result)
    {
        if (StatusText != null)
        {
            StatusText.text = result;
            _StaticStatus = StatusText.text;

            ProcessResult(result);
        }
    }

    private void ProcessResult(string result)
    {
        if (result.ToLower().IndexOf("forward") > -1)
        {
            ThrustersForwardTimed(_MovementDuration);
        }

        if (result.ToLower().IndexOf("reverse") > -1)
        {
            ThrustersReverseTimed(_MovementDuration);
        }
    }

    public void OnEndOfSpeech()
    {
        
    }

    public void OnError(string error)
    {
        if (StatusText != null)
        {
            StatusText.text = "IDLE - VOICE COMMAND ERROR: " + error;
            _StaticStatus = StatusText.text;
        }
    }
    #endregion

    #region Game update
    void Update()
    {
        if (_EnginesEnabled)
        {
            if (Spacecraft != null && BM != null && BMTwo != null)
            {
                Vector3 Velocity = (Spacecraft.transform.position - _PreviousPosition) / Time.deltaTime;
                _PreviousPosition = Spacecraft.transform.position;

                if (Time.time > _NextActionTime)
                {
                    _NextActionTime += _Period;
                    VelocityText.text = ((int)(100 * Velocity.magnitude)).ToString();
                }

                if (!string.IsNullOrEmpty(_StaticStatus))
                {
                    if (StatusText != null)
                    {
                        StatusText.text = _StaticStatus;
                    }
                }

                string Target = CheckIsWithinTarget(Spacecraft, SpacecraftDockingMechanism, MagellanDockingMechanism);
                if (TargetText != null)
                {
                    TargetText.text = Target;
                }

                if (IsWithinBoundary(Spacecraft, BM, BMTwo)) 
                {
                    if (_ThrustersForwardEnabled && !_ThrustersReverseEnabled)
                    {
                        Spacecraft.transform.Translate(Vector3.forward * _ThrustersSpeed * Time.deltaTime);

                        if (StatusText != null)
                        {
                            StatusText.text = "IN TRANSIT";
                        }
                    }
                }
                else
                {
                    if (StatusText != null)
                    {
                        StatusText.text = "MISSION BOUNDARY";
                    }
                }

                if (!_ThrustersForwardEnabled && _ThrustersReverseEnabled)
                {
                    Spacecraft.transform.Translate(Vector3.back * _ThrustersSpeed * Time.deltaTime);

                    if (StatusText != null)
                    {
                        StatusText.text = "IN TRANSIT";
                    }
                }

                if (!_YawLeftEnabled && _YawRightEnabled)
                {
                    Spacecraft.transform.Rotate(Vector3.up, _YawSpeed * Time.deltaTime);

                    if (StatusText != null)
                    {
                        StatusText.text = "REORIENTING";
                    }
                }

                if (!_YawRightEnabled && _YawLeftEnabled)
                {
                    Spacecraft.transform.Rotate(Vector3.up, -_YawSpeed * Time.deltaTime);

                    if (StatusText != null)
                    {
                        StatusText.text = "REORIENTING";
                    }
                }

                if (!_PitchUpEnabled && _PitchDownEnabled)
                {
                    Spacecraft.transform.Rotate(Vector3.right, _PitchSpeed * Time.deltaTime);

                    if (StatusText != null)
                    {
                        StatusText.text = "REORIENTING";
                    }
                }

                if (!_PitchDownEnabled && _PitchUpEnabled)
                {
                    Spacecraft.transform.Rotate(Vector3.right, -_PitchSpeed * Time.deltaTime);

                    if (StatusText != null)
                    {
                        StatusText.text = "REORIENTING";
                    }
                }
            }

            if (Asteroid != null)
            {
                Asteroid.transform.Rotate(-_AsteroidRotateSpeed * Time.deltaTime, 0, 0);
                Asteroid.transform.Translate(_AsteroidZoomSpeed * Time.deltaTime, 0, 0);
            }

            this.VerifyDocked();

            this.PlayThrusterSound(_YawRightEnabled || _YawLeftEnabled 
                || _PitchDownEnabled || _PitchUpEnabled 
                || _ThrustersForwardEnabled || _ThrustersReverseEnabled);
        }
    }
    #endregion

    #region Gameplay functions
    private string CheckIsWithinTarget(GameObject spacecraft, GameObject spacecraftDockingMechanism, GameObject magellanDockingMechanism)
    {
        if (spacecraft != null)
        {
            if (Normalize((int)spacecraft.transform.eulerAngles.x) >= -15 && Normalize((int)spacecraft.transform.eulerAngles.x) <= 15)
            {
                if (SpacecraftDockingMechanism != null)
                {
                    if (Normalize((int)spacecraft.transform.eulerAngles.y) >= -25 && Normalize((int)spacecraft.transform.eulerAngles.y) <= 10)
                    {
                        return "[STATION]";
                    }
                }

                if (magellanDockingMechanism != null)
                {
                    if (Normalize((int)spacecraft.transform.eulerAngles.y) >= 160 && Normalize((int)spacecraft.transform.eulerAngles.y) <= 200)
                    {
                        return "[MAGELLAN]";
                    }
                }
            }
        }

        return "[NONE]";
    }

    private bool IsWithinBoundary(GameObject spacecraft, GameObject bm, GameObject bmtwo)
    {
        if (spacecraft != null && bm != null && bmtwo != null)
        {
            return spacecraft.transform.position.x >= bm.transform.position.x 
                && spacecraft.transform.position.z <= bm.transform.position.z 
                && spacecraft.transform.position.z >= bmtwo.transform.position.z;
        }

        return false;
    }

    public void ResetScene()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void VerifyDocked()
    {
        if (SpacecraftDockingMechanism != null)
        {
            if (MagellanDockingMechanism != null)
            {
                if (Mathf.Abs(MagellanDockingMechanism.transform.position.x - SpacecraftDockingMechanism.transform.position.x) <= _DockingTolerance
                    && Mathf.Abs(MagellanDockingMechanism.transform.position.y - SpacecraftDockingMechanism.transform.position.y) <= _DockingTolerance
                    && Mathf.Abs(MagellanDockingMechanism.transform.position.z - SpacecraftDockingMechanism.transform.position.z) <= _DockingTolerance)
                {
                    _DockedToMagellan = true;
                    _EnginesEnabled = false;

                    if (ResetSwitch != null)
                        ResetSwitch.SetActive(true);

                    if (DockedAudioSource != null)
                        DockedAudioSource.Play();

                    GetRewardForInterceptingMagellan();
                }
            }

            if (StationDockingMechanism != null)
            {
                if (Mathf.Abs(StationDockingMechanism.transform.position.x - SpacecraftDockingMechanism.transform.position.x) <= _DockingTolerance
                    && Mathf.Abs(StationDockingMechanism.transform.position.y - SpacecraftDockingMechanism.transform.position.y) <= _DockingTolerance
                    && Mathf.Abs(StationDockingMechanism.transform.position.z - SpacecraftDockingMechanism.transform.position.z) <= _DockingTolerance)
                {
                    _DockedToStation = true;
                    _EnginesEnabled = false;

                    if (ResetSwitch != null)
                        ResetSwitch.SetActive(true);

                    if (DockedAudioSource != null)
                        DockedAudioSource.Play();

                    GetRewardForDocking();
                }
            }
        }
    }

    private void GetRewardForDocking()
    {
        GetReward(1.25f);

        if (StatusText != null)
        {
            StatusText.text = "DOCKED. THRUST UPGRADED BY 25%";
            _StaticStatus = StatusText.text;
        }
    }

    private void GetRewardForInterceptingMagellan()
    {
        GetReward(1.5f);

        if (StatusText != null)
        {
            StatusText.text = "CAPTURE. THRUST UPGRADED BY 50%";
            _StaticStatus = StatusText.text;
        }
    }

    private void GetReward(float rewardfactor)
    {
        if (rewardfactor > 0 && rewardfactor < 10)
        {
            _ThrustPercentage = _ThrustPercentage * rewardfactor;

            ThrustUpgradeText.text = ((int)((_ThrustPercentage - 1) * 100)).ToString() + "%";

            if (_ThrustersSpeed < 10)
                _ThrustersSpeed = rewardfactor * _ThrustersSpeed;
            if (_YawSpeed < 10)
                _YawSpeed = rewardfactor * _YawSpeed;
            if (_PitchSpeed < 10)
                _PitchSpeed = rewardfactor * _PitchSpeed;
        } 
    }

    private void PlayThrusterSound(bool enable)
    {
        if (ThrusterAudioSource != null)
        {
            if (enable)
            {
                if (!ThrusterAudioSource.isPlaying)
                {
                    ThrusterAudioSource.Play();
                }
            }
            else
            {
                if (ThrusterAudioSource.isPlaying)
                {
                    ThrusterAudioSource.Stop();
                }
            }
        }
    }

    public void YawRight(bool enable)
    {
        _YawRightEnabled = enable;
    }

    public void YawLeft(bool enable)
    {
        _YawLeftEnabled = enable;
    }

    public void PitchUp(bool enable)
    {
        _PitchUpEnabled = enable;
    }

    public void PitchDown(bool enable)
    {
        _PitchDownEnabled = enable;
    }

    public void ThrustersForward(bool enable)
    {
        _ThrustersForwardEnabled = enable;
    }

    public void ThrustersReverse(bool enable)
    {
        _ThrustersReverseEnabled = enable;
    }

    private void ThrustersForwardTimed(int seconds)
    {
        _ThrustersForwardEnabled = true;
        this.Invoke("AllStop", seconds);
    }

    private void ThrustersReverseTimed(int seconds)
    {
        _ThrustersReverseEnabled = true;
        this.Invoke("AllStop", seconds);
    }

    private void AllStop()
    {
        _ThrustersForwardEnabled = false;
        _ThrustersReverseEnabled = false;
    }

    public void OnToggleSpeechRecognition()
    {
        if (SpeechRecognizer.IsRecording())
        {
            SpeechRecognizer.StopIfRecording();

            if (StatusText != null)
            {
                StatusText.text = "IDLE - VOICE COMMAND INPUT STOPPED";
                _StaticStatus = StatusText.text;
            }
        }
        else
        {
            SpeechRecognizer.StartRecording(true);

            if (StatusText != null)
            {
                StatusText.text = "IDLE - VOICE COMMAND INPUT INITIALIZED";
                _StaticStatus = StatusText.text;
            }
        }
    }
    #endregion

    #region Helper functions
    private int Normalize(int deg)
    {
        int normalizedDeg = deg % 360;

        if (normalizedDeg <= -180)
            normalizedDeg += 360;
        else if (normalizedDeg > 180)
            normalizedDeg -= 360;

        return normalizedDeg;
    }
    #endregion
}
