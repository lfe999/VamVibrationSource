using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LFE
{
    public class VibrationSource : MVRScript
    {
        JSONStorableFloat vibrationFrequencyStorable;
        JSONStorableFloat vibrationStrengthXStorable;
        JSONStorableFloat vibrationStrengthYStorable;
        JSONStorableFloat vibrationStrengthZStorable;
        JSONStorableFloat vibrationDistanceStorable;
        JSONStorableStringChooser vibrationSourceStorable;

        RigidbodyVibrator selectedVibrator = null;

        public override void Init()
        {
            // create storables
            vibrationStrengthXStorable = new JSONStorableFloat(
                "Vibration Strength X",
                RigidbodyVibrator.DEFAULT_VIB_STRENGTH,
                (float val) => {
                    if(selectedVibrator != null) {
                        selectedVibrator.StrengthX = val;
                    }
                },
                RigidbodyVibrator.MIN_VIB_STRENGTH, RigidbodyVibrator.MAX_VIB_STRENGTH, constrain: false);
            RegisterFloat(vibrationStrengthXStorable);

            vibrationStrengthYStorable = new JSONStorableFloat(
                "Vibration Strength Y",
                RigidbodyVibrator.DEFAULT_VIB_STRENGTH,
                (float val) => {
                    if(selectedVibrator != null) {
                        selectedVibrator.StrengthY = val;
                    }
                },
                RigidbodyVibrator.MIN_VIB_STRENGTH, RigidbodyVibrator.MAX_VIB_STRENGTH, constrain: false);
            RegisterFloat(vibrationStrengthYStorable);

            vibrationStrengthZStorable = new JSONStorableFloat(
                "Vibration Strength Z",
                RigidbodyVibrator.DEFAULT_VIB_STRENGTH,
                (float val) => {
                    if(selectedVibrator != null) {
                        selectedVibrator.StrengthZ = val;
                    }
                },
                RigidbodyVibrator.MIN_VIB_STRENGTH, RigidbodyVibrator.MAX_VIB_STRENGTH, constrain: false);
            RegisterFloat(vibrationStrengthZStorable);

            var rigidBodyNames = GetRigidBodyMenuItems();
            vibrationSourceStorable = new JSONStorableStringChooser(
                "Vibration Source",
                rigidBodyNames,
                rigidBodyNames.Count > 1 ? rigidBodyNames[1] : rigidBodyNames[0],
                "Vibration Source",
                (string val) => {
                    SetSelectedVibrator(val);
                });
            RegisterStringChooser(vibrationSourceStorable);

            vibrationDistanceStorable = new JSONStorableFloat(
                "Vibration Distance",
                5,
                (float val) => {
                    if(selectedVibrator != null) {
                        selectedVibrator.Radius = val / 100;
                    }
                },
                0, 100.0f, constrain: false);
            RegisterFloat(vibrationDistanceStorable);

            vibrationFrequencyStorable = new JSONStorableFloat(
                "Vibrations per Second",
                RigidbodyVibrator.DEFAULT_VIB_FREQUENCY,
                (float val) => {
                    if(selectedVibrator != null) {
                        selectedVibrator.PerSecond = val;
                    }
                },
                RigidbodyVibrator.MIN_VIB_FREQUENCY, RigidbodyVibrator.MAX_VIB_FREQUENCY, constrain: false);
            RegisterFloat(vibrationFrequencyStorable);

            // Create UI
            CreateScrollablePopup(vibrationSourceStorable);
            CreateSlider(vibrationDistanceStorable);
            CreateSlider(vibrationFrequencyStorable);

            CreateSlider(vibrationStrengthXStorable, rightSide: true);
            CreateSlider(vibrationStrengthYStorable, rightSide: true);
            CreateSlider(vibrationStrengthZStorable, rightSide: true);

            SetSelectedVibrator(vibrationSourceStorable.val);

            // wait for any rigid bodies that might be late arriving (CustomUnityAsset atoms do this)
            StartCoroutine(WaitForAnyRigidbodies());

        }

        IEnumerator NudgeAtom() {
            var currentPos = containingAtom.mainController.transform.position;
            var newPos = new Vector3(currentPos.x + 0.001f, currentPos.y + 0.001f, currentPos.z + 0.001f);
            yield return new WaitForSeconds(0.05f);
            containingAtom.mainController.transform.position = newPos;
            yield return new WaitForSeconds(0.10f);
            containingAtom.mainController.transform.position = currentPos;
        }

        IEnumerator WaitForAnyRigidbodies() {
            var rigidbodies = GetRigidBodies().ToList();
            while(rigidbodies.Count == 0) {
                yield return new WaitForSeconds(0.2f);
                rigidbodies = GetRigidBodies().ToList();
            }
            // SuperController.LogMessage("rigid bodies found");
            if(vibrationSourceStorable != null) {
                var menuItems = GetRigidBodyMenuItems().ToList();
                vibrationSourceStorable.choices = menuItems;
                vibrationSourceStorable.displayChoices = menuItems;
                SetSelectedVibrator(vibrationSourceStorable.val);
            }
        }

        public void OnDisable() {
            if(selectedVibrator != null) {
                selectedVibrator.Pause = true;
            }
        }

        public void OnEnable() {
            if(selectedVibrator != null) {
                selectedVibrator.Pause = false;
            }
        }

        public void OnDestroy()
        {
            if(selectedVibrator != null) {
                Destroy(selectedVibrator);
                selectedVibrator = null;
            }
        }

        private List<string> GetRigidBodyMenuItems() {
            var rigidBodyNames = GetRigidBodies().Select(r => r.name).ToList();
            rigidBodyNames.Insert(0, String.Empty);
            return rigidBodyNames;
        }

        private IEnumerable<Rigidbody> GetRigidBodies() {
            var rescale = containingAtom.reParentObject.Find("object/rescaleObject");
            // SuperController.LogMessage($"rescale: {rescale}");
            foreach(var c in rescale.GetComponentsInChildren<Rigidbody>()) {
                // SuperController.LogMessage($"{c}");
            }
            // foreach(var c in containingAtom.GetComponentsInChildren<Component>()) {
            //     SuperController.LogMessage($"{c}");
            // }
            foreach(var rb in containingAtom.GetComponentsInChildren<Rigidbody>()) {
                yield return rb;
            }
        }

        private void SetSelectedVibrator(string rigidBodyName) {
            if(selectedVibrator != null) {
                Destroy(selectedVibrator);
                selectedVibrator = null;
            }
            var rb = GetRigidBodies().FirstOrDefault(r => r.name == rigidBodyName);
            if(rb != null) {
                var v = containingAtom.gameObject.AddComponent<RigidbodyVibrator>();
                v.Rigidbody = rb;
                v.StrengthX = vibrationStrengthXStorable.val;
                v.StrengthY = vibrationStrengthYStorable.val;
                v.StrengthZ = vibrationStrengthZStorable.val;
                v.PerSecond = vibrationFrequencyStorable.val;
                v.Radius = vibrationDistanceStorable.val / 100;
                selectedVibrator = v;

                // move just a bit to trigger collision on load
                StartCoroutine(NudgeAtom());
            }
        }
    }

    public class RigidbodyVibrator : MonoBehaviour {

        public const float DEFAULT_VIB_STRENGTH = 25;
        public const float MIN_VIB_STRENGTH = 0;
        public const float MAX_VIB_STRENGTH = 100;

        public const float DEFAULT_VIB_FREQUENCY = 25;
        public const float MIN_VIB_FREQUENCY = 0;
        public const float MAX_VIB_FREQUENCY = 200;

        public bool Pause { get; set; } = false;

        private float _radius = 0;
        public float Radius {
            get {
                return _radius;
            }
            set {
                _radius = value;
                if(_logger != null){
                    _logger.Radius = value;
                }
            }
        }

        public bool Enabled { get; set; }

        private float _strength;
        public float Strength {
            set {
                _strength = value;
                StrengthX = _strength;
                StrengthY = _strength;
                StrengthZ = _strength;
            }
        }
        public float StrengthX { get; set; }
        public float StrengthY { get; set; }
        public float StrengthZ { get; set; }
        public float PerSecond { get; set; }

        private CollisionLogger _logger;
        private Rigidbody _rigidbody;
        public Rigidbody Rigidbody {
            get {
                return _rigidbody;
            }
            set {
                if(_logger != null) {
                    // SuperController.LogMessage($"destroying logger {_logger}");
                    Destroy(_logger);
                }
                _rigidbody = value;
                if(_rigidbody != null){
                    _logger = _rigidbody.gameObject.AddComponent<CollisionLogger>();
                    Enabled = true;
                    Strength = DEFAULT_VIB_STRENGTH;
                    PerSecond = DEFAULT_VIB_FREQUENCY;
                }
            }
        }
        public string Name => Rigidbody.name;

        int _direction = -1;
        float _waited = 0;
        public void FixedUpdate() {
            if(SuperController.singleton.freezeAnimation || Pause || !Enabled || _rigidbody == null || _logger == null || PerSecond <= 0) {
                return;
            }

            // only run based on the per second frequency
            _waited += Time.fixedDeltaTime;
            var delay = 1/PerSecond;
            if(_waited < delay) {
                return;
            }
            _waited = 0;

            if(StrengthX > 0 || StrengthY > 0 || StrengthZ > 0) {
                _direction *= -1;
                var strengthX = StrengthX * _direction / 50;
                var strengthY = StrengthY * _direction / 50;
                var strengthZ = StrengthZ * _direction / 50;

                var rbForceApplied = new Dictionary<string, bool>();
                foreach(var item in _logger.Contacts) {
                    var touching = item.Value;

                    if(rbForceApplied.ContainsKey(touching.name)) {
                        continue;
                    }
                    touching.AddRelativeForce(strengthX, strengthY, strengthZ, ForceMode.VelocityChange);
                    rbForceApplied[touching.name] = true;

                    if(_logger.Radius > 0) {
                        if(_logger.Nearby.ContainsKey(item.Key)) {
                            // var bucketWasRun = new Dictionary<int, bool>();
                            foreach(var near in _logger.Nearby[item.Key]) {
                                if(near.name == touching.name) {
                                    continue;
                                }

                                if(rbForceApplied.ContainsKey(near.name)) {
                                    continue;
                                }

                                var distance = Vector3.Distance(near.position, touching.position);
                                var pct = Mathf.Max(0, 1 - (distance / _logger.Radius));

                                // only add a force once per bucket -- this prevents dense clusters of colliders from being over exaggerated
                                // int bucket = (int)((pct * 100) / 2);

                                // if(bucketWasRun.ContainsKey(bucket)) {
                                    // continue;
                                // }

                                // easeoutquint - https://easings.net/#easeOutQuint
                                var falloff = 1 - Mathf.Pow(1 - pct, 3);

                                if(falloff > 0) {
                                    near.AddRelativeForce(strengthX * falloff, strengthY * falloff, strengthZ * falloff, ForceMode.VelocityChange);
                                    rbForceApplied[near.name] = true;
                                    // bucketWasRun[bucket] = true;
                                }
                            }
                        }
                    }
                }
            }
        }

        public void OnDestroy() {
            Rigidbody = null; // this handles Destroy / cleanup too
        }
    }

    public class CollisionLogger : MonoBehaviour {
        
        public Dictionary<string, Rigidbody> Contacts = new Dictionary<string, Rigidbody>();
        public Dictionary<string, List<Rigidbody>> Nearby = new Dictionary<string, List<Rigidbody>>();
        
        private List<Collider> _allColliders = new List<Collider>();

        private float _radius = 0;
        public float Radius {
            get {
                return _radius;
            }
            set {
                Nearby = new Dictionary<string, List<Rigidbody>>();
                _radius = value;
            }
        }

        public void OnCollisionEnter(Collision collision) {
            Contacts[collision.rigidbody.name] = collision.rigidbody;
            SetNearByColliders(collision);
        }

        private void SetNearByColliders(Collision collision, bool skipTheCache = false) {
            if(Radius > 0) {
                if(!skipTheCache && Nearby.ContainsKey(collision.rigidbody.name)) {
                    return;
                }
                var nearby = new List<Rigidbody>();
                if(_allColliders.Count == 0) {
                    _allColliders = collision.transform.root.GetComponentsInChildren<Collider>().ToList();
                }
                foreach(var collider in _allColliders) {
                    if(collider.attachedRigidbody != null) {
                        var distance = Vector3.Distance(collision.rigidbody.position, collider.attachedRigidbody.position);
                        if(distance <= Radius) {
                            nearby.Add(collider.attachedRigidbody);
                        }
                    }
                }
                Nearby[collision.rigidbody.name] = nearby;
            }
        }

        public void OnCollisionExit(Collision collision) {
            Contacts.Remove(collision.rigidbody.name);
            // Nearby.Remove(collision.rigidbody.name);
        }

        public void OnCollisionStay(Collision collision) {
            Contacts[collision.rigidbody.name] = collision.rigidbody;
            // just pay attention to "stay" if it looks like the plugin was just initialized
            if(!Nearby.ContainsKey(collision.rigidbody.name)) {
                SetNearByColliders(collision);
            }
        }

        public void OnDestroy() {
            Contacts = new Dictionary<string, Rigidbody>();
            Nearby = new Dictionary<string, List<Rigidbody>>();
        }
    }
}
