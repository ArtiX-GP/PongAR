using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vuforia;

public class MainScript : MonoBehaviour, ITrackableEventHandler {

    const float P = 1;
    const float I = 0;
    const float D = 0.2f;

    const float IMAX = 5;
    const float IMIN = -5;

    private static float _IState, _DState;

    struct Line {
        public Vector2 _Start;
        public Vector2 _End;
    }

    class Ball {

        private Vector2 _Position;

        private Vector2 _Velocity;

        private int _Radius = 10;

        public Platform _PL;

        public Platform _PR;

        public Ball() {
            _Velocity = new Vector2(Random.Range(-3, 3), Random.Range(-3, 3));
        }

        public void Update() {
            _Position += _Velocity;
            CheckBoundaries();
            CheckPlatformCollision();
        }

        public void NewVelocity() {
            _Velocity = new Vector2(Random.Range(-3, 3), Random.Range(-3, 3));
        }

        private void CheckBoundaries() {
            if (_PL != null) {
                if (_Position.x <= _PL.GetPosition().x - _Radius || _Position.x < _LeftUpPoint.x) {
                    // Пропуск с левой стороны.
                    BallConceded(false);
                }
            }

            if (_PR != null) {
                if (_Position.x >= _PR.GetPosition().x + _PR.GetWidth() || _Position.x > _RightDownPoint.x) {
                    // Пропуск с правой стороны.
                    BallConceded(true);
                }
            }

            if (_Position.y >= _LeftUpPoint.y || _Position.y <= _RightDownPoint.y + _Radius) {
                _Velocity.y *= -1;
                _Velocity += new Vector2(Random.Range(-0.05f, 0.05f), Random.Range(-0.05f, 0.05f));
            }
        }

        public void Reset() {
            float centerX = _LeftUpPoint.x + (_RightDownPoint.x - _LeftUpPoint.x) / 2;
            float centerY = _LeftUpPoint.y + (_RightDownPoint.y - _LeftUpPoint.y) / 2;
            _Ball._Position = new Vector2(centerX, centerY);
            NewVelocity();
        }

        private void CheckPlatformCollision() {
            bool isInRight = _Position.x > Screen.width / 2;
            if (isInRight) {
                if ((_Position.x >= _PR.GetPosition().x)
                    && (_Position.x <= _PR.GetPosition().x + _PR.GetWidth())
                    && (_Position.y <= _PR.GetPosition().y)
                    && (_Position.y >= _PR.GetPosition().y - _PR.GetHeight())) {
                    _Position.x = _PR.GetPosition().x;
                    _Velocity.x = -_Velocity.x;
                }
            } else {
                if ((_Position.x >= _PL.GetPosition().x)
                    && (_Position.x <= _PL.GetPosition().x + _PL.GetWidth())
                    && (_Position.y <= _PL.GetPosition().y)
                    && (_Position.y >= _PL.GetPosition().y - _PL.GetHeight())) {
                    _Position.x = _PL.GetPosition().x + _PL.GetWidth();
                    _Velocity.x = -_Velocity.x;
                }
            }
        }

        public void SetPosition(Vector2 pos) {
            _Position = pos;
            _Position.x -= _Radius;
            _Position.y -= _Radius;
        }

        public Vector2 GetPosition() {
            return _Position;
        }

        public void Draw() {
            DrawQuad(new Rect(_Position.x, _Position.y, _Radius, _Radius), Color.green);
        }
    }

    class Platform {

        private Vector2 _Position;

        private int _Width = 36;

        private int _Height = 128;

        private Vector2 _Velocity;

        const float ACC_MIN = -20f;
        const float ACC_MAX = 20f;

        public Platform(Vector2 pos) {
            _Position = pos;
            _Velocity = new Vector2(0, 0);
        }

        public void Update() {
            _Position += _Velocity;
            CheckBoundaries();
        }

        private void CheckBoundaries() {
            if (_Position.y > _LeftUpPoint.y) {
                _Position.y = _LeftUpPoint.y;
            }

            if ((_Position.y - _Height) < _RightDownPoint.y) {
                _Position.y = _RightDownPoint.y + _Height;
            }
        }

        public void KeepTarget() {
            float e =  -(_Position.y - _Ball.GetPosition().y);
            print("e = " + e);


            float pTerm = P * e;

            _IState = _IState + e;
            if (_IState < IMIN) {
                _IState = IMIN;
            }

            if (_IState > IMAX) {
                _IState = IMAX;
            }

            float iTerm = I * _IState;

            float dTerm = D * (e - _DState);
            _DState = e;

            float acc = pTerm + iTerm - dTerm;
            if (acc < ACC_MIN) {
                acc = ACC_MIN;
            }
            if (acc > ACC_MAX) {
                acc = ACC_MAX;
            }

            _Velocity.y = acc;
        }

        public void Draw() {
            DrawQuad(new Rect(_Position.x, _Position.y, _Width, _Height), _Color);
        }

        public void SetPosition(Vector2 pos) {
            _Position = pos;
        }

        public Vector2 GetPosition() {
            return _Position;
        }

        public int GetWidth() {
            return _Width;
        }

        public int GetHeight() {
            return _Height;
        }

    }

    // Левая верхняя и правая нижняя точки игрового поля.
    private static Vector2 _LeftUpPoint, _RightDownPoint;

    // Цвет кисти.
    private static Color _Color = Color.red;

    // Счет левой и правой стороны.
    private static int _LS, _RS;

    // Маркер.
    public TrackableBehaviour _Target;

    private Vector3 _StartTargetPositionScreen;

    private Vector3 _TargetPositionScreen;

    public Camera _Camera;

    private bool isTargetFound;

    private bool isGameStarted;

    private static Ball _Ball;

    private Platform _PlatformLeft, _PlatformRight;

    // Start is called before the first frame update
    void Start() {
        //_LeftTarget = GetComponent<TrackableBehaviour>();
        if (_Target) {
			_Target.RegisterTrackableEventHandler(this);
        }

        _PlatformLeft = new Platform(new Vector2(0, 0));
        _PlatformRight = new Platform(new Vector2(0, 0));

        _Ball = new Ball();
        _Ball._PL = _PlatformLeft;
        _Ball._PR = _PlatformRight;
    }

     public void OnTrackableStateChanged(TrackableBehaviour.Status previousStatus, TrackableBehaviour.Status newStatus) {
        Debug.Log(newStatus);
        if (newStatus == TrackableBehaviour.Status.DETECTED ||
            newStatus == TrackableBehaviour.Status.TRACKED ||
            newStatus == TrackableBehaviour.Status.EXTENDED_TRACKED) {
            isTargetFound = true;

            _StartTargetPositionScreen = _Camera.WorldToScreenPoint(_Target.transform.position);
        } else {
            isTargetFound = false;
            isGameStarted = false;
        }
    }	

    void OnGUI() {
        if (isTargetFound) {
            if (GUI.Button(new Rect(10, 10, 100, 60), (isGameStarted ? "Press to stop!" : "Press to start!"))) {
                if (_Target) {
                    _Ball.NewVelocity();

                    _StartTargetPositionScreen = _Camera.WorldToScreenPoint(_Target.transform.position);
                    float deltaX = Screen.width - _StartTargetPositionScreen.x;
                    float deltaY = Screen.height - 20;
                    _LeftUpPoint = new Vector2(deltaX, deltaY);
                    _RightDownPoint = new Vector2(_StartTargetPositionScreen.x, 20);

                    _Ball.Reset();

                    float centerY = _LeftUpPoint.y + (_RightDownPoint.y - _LeftUpPoint.y) / 2;
                    _PlatformLeft.SetPosition(new Vector2(_LeftUpPoint.x + 30, centerY));
                }
                isGameStarted = !isGameStarted;
                _LS = _RS = 0;
            }

            if (GUI.Button(new Rect(125, 10, 100, 60), "Reset scores!")) {
                _LS = _RS = 0;
            }

            DrawQuad(new Rect(_TargetPositionScreen.x, _TargetPositionScreen.y, 25, 25), Color.red);
        }

        if (isGameStarted) {
            if (_StartTargetPositionScreen.x < Screen.width / 2) {
                print("Place target at right side!");
                return;
            }

            _Color = Color.white;

            // float deltaX = Screen.width - _StartTargetPositionScreen.x;
            // DrawLine(new Vector2(deltaX, 20), new Vector2(_StartTargetPositionScreen.x, 20)); // НГ
            // float deltaY = Screen.height - 20;
            // DrawLine(new Vector2(deltaX, deltaY), new Vector2(_StartTargetPositionScreen.x, deltaY)); // ВГ
            // DrawLine(new Vector2(deltaX, 20), new Vector2(deltaX, deltaY)); // ЛВ
            // DrawLine(new Vector2(_StartTargetPositionScreen.x, 20), new Vector2(_StartTargetPositionScreen.x, deltaY)); // ПВ
            DrawField();

            // Отрисовка мячика.
            _Ball.Draw();

            // Отрисовка NPC.
            _PlatformLeft.Draw();

            // Отрисовка счёта.
            float centerX = _LeftUpPoint.x + (_RightDownPoint.x - _LeftUpPoint.x) / 2;
            GUIStyle guiStyle = new GUIStyle();
            guiStyle.fontSize = 36;
            guiStyle.normal.textColor = _Color;
            GUI.Label (new Rect(centerX - 40, 50, 100, 30), "" + _LS, guiStyle);
            GUI.Label (new Rect(centerX + 20, 50, 100, 30), "" + _RS, guiStyle);

            _Color = Color.red;
            _PlatformRight.Draw();
           //isFieldDrawn = !isFieldDrawn;
        }
        //DrawLine(_StartTargetPositionScreen, new Vector2(100, 100));
    }

    // Update is called once per frame
    void Update() {
        // if (_LeftTarget) {
        //     if (_LeftTarget.CurrentStatus == TrackableBehaviour.Status.TRACKED ||
        //         _LeftTarget.CurrentStatus == TrackableBehaviour.Status.DETECTED ||
        //         _LeftTarget.CurrentStatus == TrackableBehaviour.Status.EXTENDED_TRACKED) {
        //             isLeftFounded = true;
        //         } else {
        //             isLeftFounded = false;
        //         }
        // } else {
        //     isLeftFounded = false;
        // }

        // if (_RightTarget) {
        //     if (_RightTarget.CurrentStatus == TrackableBehaviour.Status.TRACKED ||
        //         _RightTarget.CurrentStatus == TrackableBehaviour.Status.DETECTED ||
        //         _RightTarget.CurrentStatus == TrackableBehaviour.Status.EXTENDED_TRACKED) {
        //             isRightFounded = true;
        //         } else {
        //             isRightFounded = false;
        //         }
        // } else {
        //     isRightFounded = false;
        // }

        // if (isLeftFounded && isRightFounded) {
        //     Debug.Log("Start")
        // }

        if (_Target) {
            _TargetPositionScreen = _Camera.WorldToScreenPoint(_Target.transform.position);
            if (_TargetPositionScreen.x < Screen.width / 2) {
                _TargetPositionScreen.x = Screen.width / 2;
            }

            if (_PlatformRight != null) {
                _PlatformRight.Update();
                _PlatformRight.SetPosition(new Vector2(_TargetPositionScreen.x -+_PlatformRight.GetWidth() / 2, 
                                           _TargetPositionScreen.y + _PlatformRight.GetHeight() / 2));
            }

            if (_PlatformLeft != null) {
                _PlatformLeft.KeepTarget();
                _PlatformLeft.Update();
            }

            _Ball.Update();
           // Debug.Log(screenPos);
        }
    }

    void DrawField() {
        // Нижняя граница.
        Vector2 leftDownPoint = _LeftUpPoint;
        leftDownPoint.y = _RightDownPoint.y;
        DrawLine(_RightDownPoint, leftDownPoint);

        // Верхняя граница.
        Vector2 rightUpPoint = _RightDownPoint;
        rightUpPoint.y = _LeftUpPoint.y;
        DrawLine(_LeftUpPoint, rightUpPoint);

        // Левая граница.
        DrawLine(leftDownPoint, _LeftUpPoint);

        // Правая граница.
        DrawLine(_RightDownPoint, rightUpPoint);

        // Центральная граница.
        float centerX = _LeftUpPoint.x + (_RightDownPoint.x - _LeftUpPoint.x) / 2;
        DrawLine(new Vector2(centerX, 0), new Vector2(centerX, Screen.height));
    }

    Vector2 FormatPoint(Vector2 point) {
		point.x = (int) point.x;
		point.y = Screen.height - (int)point.y;
		return point;
	}

    void DrawLine(Vector2 pointA, Vector2 pointB) {
		pointA = FormatPoint(pointA);
		pointB = FormatPoint(pointB);
		Texture2D lineTex = new Texture2D (1, 1);  
		Matrix4x4 matrixBackup = GUI.matrix; 
		float width = 8.0f; 	 	   	
		GUI.color = _Color; 		
		float angle = Mathf.Atan2 (pointB.y - pointA.y, pointB.x - pointA.x) * 180f / Mathf.PI;

		GUIUtility.RotateAroundPivot (angle, pointA);
		GUI.DrawTexture (new Rect (pointA.x, pointA.y, (pointB - pointA).magnitude, width), lineTex);
		GUI.matrix = matrixBackup;  
	}

    public static void DrawQuad(Rect position, Color color) {
        position.y = Screen.height - (int)position.y;
        Texture2D texture = new Texture2D(1, 1);
        texture.SetPixel(0,0,color);
        texture.Apply();
        GUI.skin.box.normal.background = texture;
        GUI.Box(position, GUIContent.none);
    }

    public static void BallConceded(bool isRightSide) {
        if (isRightSide) {
            _LS++;
        } else {
            _RS++;
        }

        _Ball.Reset();
    }

}
