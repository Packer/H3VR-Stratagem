using UnityEngine;
using FistVR;
using Sodalite.Api;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.AI;


namespace Stratagem
{
    public class Stratagem_Grenade : FVRPhysicalObject
    {
        [Header("Stratagem")]
        public string currentInput = "";

        private Stratagem_Package selected;
        private bool callingIn = false;

        [Header("Visuals")]
        public Animator ballAnimator;
        public LineRenderer beam;
        public MeshRenderer ballGlow;
        public LineRenderer lightGlow;

        public Material[] ballGlowMats;
        public Material[] ballBeamMats;
        public Material[] ballLightMats;
        public Color[] textColors;

        [Header("Hellpod")]
        public GameObject hellpod;
        public GameObject hellpodFlame;
        public GameObject hellpodLandingEffect;
        public GameObject hellpodLandedEffect;
        public AudioEvent hellpodLand;
        public Transform[] hellpodSpawnPoints;

        [Header("UI")]
        public GameObject inputUI;
        public GameObject stratagemsUI;

        public GameObject previewPrefab;
        [HideInInspector]
        public Dictionary<Stratagem_Package, Stratagem_Preview> previews = new Dictionary<Stratagem_Package, Stratagem_Preview>();
        public Image[] arrowInputs;
        public Sprite[] arrowSprites;

        [Header("Call In UI")]
        public Transform callIn;
        public Text callTitle;
        public Text callTimer;
        public Text callDistance;
        public Image callThumbnail;
        public Image callArrow;

        private float deployHeight = 1000;
        private float deployTimer = 0;
        private float headBallDistance = 1;
        private Transform parentTarget;
        private Vector3 offset;

        public override void Start()
        {
            base.Start();

            //Default state
            for (int i = 0; i < arrowInputs.Length; i++)
            {
                arrowInputs[i].gameObject.SetActive(false);
            }
            inputUI.SetActive(false);
            stratagemsUI.SetActive(false);

            ballGlow.gameObject.SetActive(false);
            lightGlow.gameObject.SetActive(false);
            beam.gameObject.SetActive(false);
            previewPrefab.SetActive(false);
            callIn.gameObject.SetActive(false);

            hellpod.SetActive(false);
            hellpodFlame.SetActive(false);
            hellpodLandingEffect.SetActive(false);
            hellpodLandedEffect.SetActive(false);

            SetupStratagems();
        }

        void SetupStratagems()
        {

            //Setup Stratagems
            for (int i = 0; i < ModLoader.mods.Count; i++)
            {
                Stratagem_Package package = ModLoader.mods[i].package;
                //Sanity Check

                List<string> spawnIDs = new List<string>();
                if (package.spawnObjectIDs != null)
                {
                    for (int y = 0; y < package.spawnObjectIDs.Length; y++)
                    {
                        if (IM.OD.ContainsKey(package.spawnObjectIDs[y]))
                            spawnIDs.Add(package.spawnObjectIDs[y]);
                    }
                }

                //Update with valid data
                package.spawnObjectIDs = spawnIDs.ToArray();

                List<int> sosigIDs = new List<int>();
                if (package.spawnSosigEnemyIDs != null)
                {
                    for (int y = 0; y < package.spawnSosigEnemyIDs.Length; y++)
                    {
                        if (IM.Instance.odicSosigObjsByID.ContainsKey((SosigEnemyID)package.spawnSosigEnemyIDs[y]))
                            sosigIDs.Add(package.spawnSosigEnemyIDs[y]);
                    }
                }

                package.spawnSosigEnemyIDs = sosigIDs.ToArray();

                int prefabLength = 0;
                if (package.spawnPrefabs != null)
                    prefabLength = package.spawnPrefabs.Length;
                else
                    package.spawnPrefabs = [];

                int instantPrefabLength = 0;
                if (package.spawnInstantPrefab != null)
                    instantPrefabLength = package.spawnInstantPrefab.Length;
                else
                    package.spawnInstantPrefab = [];

                //If No valid content, do not include (Mostly for other mods hooking in without dependency)
                if (spawnIDs.Count == 0
                    && prefabLength == 0
                    && instantPrefabLength == 0
                    && sosigIDs.Count == 0)
                {
                    continue;
                }


                //Create our Preview
                Stratagem_Preview preview = Instantiate(previewPrefab, previewPrefab.transform.parent).GetComponent<Stratagem_Preview>();
                preview.gameObject.SetActive(true);


                preview.title.text = package.title;
                preview.description.text = package.description;
                preview.icon.sprite = package.icon;

                for (int x = 0; x < 10; x++)
                {
                    if (x < package.inputs.Length)
                        preview.arrows[x].sprite = arrowSprites[ArrowConvert(package.inputs[x].ToString())];
                    else
                        preview.arrows[x].gameObject.SetActive(false);
                }

                preview.package = package;

                previews.Add(package, preview);
            }

        }

        int ArrowConvert(string letter)
        {
            switch (letter)
            {
                default:
                case "U":
                    return 0;
                case "D":
                    return 1;
                case "L":
                    return 2;
                case "R":
                    return 3;
            }
        }

        void Update()
        {

            /*
            //Show our list of stratagems
            if (IsHeld && Vector3.Dot(-transform.forward, GM.CurrentPlayerBody.Head.forward) > 0.01f)
                stratagemsUI.SetActive(true);
            else
                stratagemsUI.SetActive(false);
            */

            if (callingIn)
            {
                deployTimer = Mathf.Clamp(deployTimer - Time.deltaTime, 0, Mathf.Infinity);

                Vector3 headBallDir = transform.position - GM.CurrentPlayerBody.Head.position;
                headBallDistance = Vector3.Magnitude(headBallDir);

                //Scale with distance
                beam.widthMultiplier = Mathf.Min(0.175f, 0.175f * headBallDistance);


                //Force drop to look at player
                Vector3 playerPos = GM.CurrentPlayerBody.Head.position;
                playerPos.y = transform.position.y;
                transform.LookAt(playerPos);

                if (deployTimer > 0.5f)
                {
                    System.TimeSpan span = System.TimeSpan.FromSeconds(Mathf.CeilToInt(deployTimer));
                    callTimer.text = "INBOUND " + string.Format("{0:00}:{1:00}", span.Minutes, span.Seconds);
                    beam.gameObject.SetActive(true);
                }
                else
                {
                    callTimer.text = "IMPACT";
                    beam.gameObject.SetActive(false);
                }

                if (selected.deployType == Stratagem_Package.DeployType.Hellpod
                    && hellpod.transform.parent == transform)
                {

                    hellpod.transform.localPosition = Vector3.Lerp(
                        Vector3.up * deployHeight,
                        Vector3.zero,
                        Mathf.InverseLerp(selected.deployTimer, 0, deployTimer));
                }
            }
            

            if (previews != null && previews.Count > 0)
            {

                foreach (Stratagem_Package package in previews.Keys)
                {
                    Stratagem_Preview preview;
                    previews.TryGetValue(package, out preview);

                    if (preview == null || preview.package == null)
                        continue;

                    //Cooldown
                    if (preview.package.timeout > Time.time)
                    {
                        preview.cooldown.gameObject.SetActive(true);
                        preview.arrowsParent.gameObject.SetActive(false);

                        System.TimeSpan span = System.TimeSpan.FromSeconds(Mathf.CeilToInt(Time.time - package.timeout));
                        preview.cooldown.text = "Cooldown T" + "-" + string.Format("{0:00}:{1:00}", span.Minutes, span.Seconds);
                    }
                    else
                    {
                        preview.cooldown.gameObject.SetActive(false);
                        preview.arrowsParent.gameObject.SetActive(true);
                    }
                }
            }

        }

        void LateUpdate()
        {
            if (!callingIn && !callIn.gameObject.activeSelf)
                return;

            //Parent
            if (parentTarget != null)
                transform.position = parentTarget.position + (parentTarget.rotation * offset);

            //Call in Display
            callIn.position = GM.CurrentPlayerBody.Head.position;
            callIn.LookAt(transform.position);
            callIn.position += callIn.forward * 2;
            callDistance.text = Mathf.FloorToInt(headBallDistance).ToString() + "m";

        }

        void SpawnPrefabs()
        {
            for (int i = 0; i < selected.spawnPrefabs.Length; i++)
            {
                if (selected.spawnPrefabs[i] != null)
                    Instantiate(selected.spawnPrefabs[i], transform.position, Quaternion.identity);
            }
        }

        void SpawnInstantPrefabs()
        {
            for (int i = 0; i < selected.spawnInstantPrefab.Length; i++)
            {
                if (selected.spawnInstantPrefab[i] != null)
                    Instantiate(selected.spawnInstantPrefab[i], transform.position, Quaternion.identity);
            }
        }

        void SpawnSosigs()
        {

            SosigAPI.SpawnOptions _spawnOptions = new SosigAPI.SpawnOptions
            {
                SpawnState = Sosig.SosigOrder.GuardPoint,
                SpawnActivated = true,
                EquipmentMode = SosigAPI.SpawnOptions.EquipmentSlots.All,
                SpawnWithFullAmmo = true,
            };

            NavMeshHit navHit;
            Vector3 position = transform.position;

            for (int i = 0; i < selected.spawnSosigEnemyIDs.Length; i++)
            {
                //Spawn All sosigs
                if (NavMesh.SamplePosition(position, out navHit, 15f, NavMesh.AllAreas))
                {
                    position = navHit.position;
                }

                //Give them a simple 4 meter squared Patrol around the drop pod
                Sosig sosig =
                SosigAPI.Spawn(
                    IM.Instance.odicSosigObjsByID[(SosigEnemyID)selected.spawnSosigEnemyIDs[i]],
                    _spawnOptions,
                    position,
                    transform.rotation);

                sosig.UpdateGuardPoint(position);
                StratagemPlugin.AddSosig(sosig);

                //Set Agents to low quailty level for a bit of a performance boost
                NavMeshAgent agent = sosig.GetComponent<NavMeshAgent>();

                agent.obstacleAvoidanceType = ObstacleAvoidanceType.LowQualityObstacleAvoidance;
                //agent.stoppingDistance = 1;

                position += Transform.forward;
            }
        }

        void SpawnObjectIDs()
        {
            //Enable special drop pod attachment here

            FVRObject mainObject;
            int spawnIndex = 0;

            for (int i = 0; i < selected.spawnObjectIDs.Length; i++)
            {

                //Try to find the weapon ID
                if (!IM.OD.TryGetValue(selected.spawnObjectIDs[i], out mainObject))
                {
                    Debug.Log("Stratagem: Cannot find object with id: " + selected.spawnObjectIDs[i]);
                    continue;
                }

                GameObject spawnedMain = null;
                if (mainObject.GetGameObject() != null)
                    spawnedMain = Instantiate(
                        mainObject.GetGameObject(),
                        hellpodSpawnPoints[spawnIndex].position + ((Vector3.up * 0.25f) * i),
                        hellpodSpawnPoints[spawnIndex].rotation);
                //TODO INSTANTIATE OBJECTS HERE IN ORDER OF TRANFORMS THEN LOOP AFTER INDEX 0


                if (spawnIndex + 1 < hellpodSpawnPoints.Length)
                    spawnIndex++;
                else
                    spawnIndex = 0;
            }
        }

        public void InputDirection(string direction)
        {
            currentInput = currentInput + direction;

            bool matchFound = false;

            for (int i = 0; i < arrowInputs.Length; i++)
            {
                if (i < currentInput.Length)
                {
                    arrowInputs[i].sprite = arrowSprites[ArrowConvert(currentInput[i].ToString())];
                    arrowInputs[i].gameObject.SetActive(true);
                }
                else
                    arrowInputs[i].gameObject.SetActive(false);
            }

            foreach (var mod in ModLoader.mods)
            {
                if (mod.package.inputs.Contains(currentInput))
                    matchFound = true;

                if (mod.package.inputs == currentInput)
                {
                    if (Time.time < mod.package.timeout)
                    {
                        ClearEvent();
                        break;
                    }

                    //COMBO COMPLETE!
                    selected = mod.package;

                    //Visual Update
                    ballGlow.sharedMaterial = ballGlowMats[(int)mod.package.deployColor];
                    beam.sharedMaterial = ballBeamMats[(int)mod.package.deployColor];
                    lightGlow.sharedMaterial = ballLightMats[(int)mod.package.deployColor];

                    ballGlow.gameObject.SetActive(true);

                    SM.PlayCoreSound(
                        FVRPooledAudioType.UIChirp,
                        AudEvent_Ready,
                        transform.position);
                    return;
                }
            }

            //Maxed out or doesn't exist
            if (currentInput.Length >= 10 || matchFound == false)
            {
                ClearEvent();
                return;
            }

            SM.PlayCoreSound(
                FVRPooledAudioType.UIChirp,
                AudEvent_Arrow,
                transform.position);
        }

        public override void BeginInteraction(FVRViveHand hand)
        {
            base.BeginInteraction(hand);

            if (inputUI.activeSelf == false)
            {
                inputUI.SetActive(true);
                stratagemsUI.SetActive(true);
            }

        }

        public override void EndInteraction(FVRViveHand hand)
        {
            base.EndInteraction(hand);
            ClearMenu();
        }

        public override void SetQuickBeltSlot(FVRQuickBeltSlot slot)
        {
            base.SetQuickBeltSlot(slot);
            ClearMenu();
        }

        void ClearMenu()
        {
            inputUI.SetActive(false);
            stratagemsUI.SetActive(false);

            if (selected == null)
            {
                ClearEvent(true);
            }
        }

        public override void EndInteractionIntoInventorySlot(FVRViveHand hand, FVRQuickBeltSlot slot)
        {
            base.EndInteractionIntoInventorySlot(hand, slot);
            ClearMenu();
        }


        public override void OnCollisionEnter(Collision col)
        {
            base.OnCollisionEnter(col);

            if (!IsHeld && QuickbeltSlot == null && selected != null && callingIn == false)
            {
                callingIn = true;
                callTitle.color = textColors[(int)selected.textColor];
                callArrow.color = textColors[(int)selected.textColor];
                callThumbnail.sprite = selected.icon;

                RootRigidbody.velocity = Vector3.zero;
                RootRigidbody.angularVelocity = Vector3.zero;
                RootRigidbody.MoveRotation(Quaternion.identity);
                RootRigidbody.isKinematic = true;
                RootRigidbody.detectCollisions = false;

                parentTarget = col.transform;
                offset = transform.position - col.transform.position;
                //Transform.SetParent(col.transform);

                selected.timeout = Time.time + selected.cooldown;
                deployTimer = selected.deployTimer; //Time until landed

                callIn.gameObject.SetActive(true);

                ballGlow.gameObject.SetActive(false);
                beam.gameObject.SetActive(true);
                lightGlow.gameObject.SetActive(true);

                IsPickUpLocked = true;
                DistantGrabbable = false;

                ballAnimator.Play("Stratagem_Open", 0);

                if (selected.deployType == Stratagem_Package.DeployType.Hellpod)
                {
                    //Move hellpod into safer valid location if it exists nearby
                    NavMeshHit navHit;
                    if (NavMesh.SamplePosition(transform.position, out navHit, 2f, NavMesh.AllAreas))
                    {
                        RootRigidbody.MovePosition(navHit.position);
                    }
                    hellpod.SetActive(true);
                    hellpod.transform.localPosition = Vector3.up * deployHeight;
                    hellpodLandingEffect.SetActive(true);
                    hellpodFlame.SetActive(true);

                    SM.PlayCoreSound(
                        FVRPooledAudioType.GenericLongRange,
                        AudEvent_HellpodDrop,
                        transform.position);
                }

                SM.PlayCoreSound(
                    FVRPooledAudioType.GenericLongRange,
                    AudEvent_Beacon,
                    transform.position);

                SpawnInstantPrefabs();

                //Self Destruct
                StartCoroutine(SelfDestruct(deployTimer));
            }
        }

        public IEnumerator SelfDestruct(float timer)
        {
            yield return new WaitForSeconds(timer);
            if (selected.deployType == Stratagem_Package.DeployType.Hellpod)
            {
                ShakeScreen();
                //Detach Hellpod object
                if (selected.deployType == Stratagem_Package.DeployType.Hellpod)
                {
                    hellpod.transform.SetParent(null, true);
                    hellpod.transform.position = transform.position;
                    //Destroy hellpod after 300 seconds for performance
                    StratagemPlugin.DestroyObject(300, hellpod);
                }
                
                SM.PlayCoreSound(
                    FVRPooledAudioType.GenericLongRange,
                    hellpodLand,
                    transform.position);

                hellpodFlame.SetActive(false);
                hellpodLandedEffect.SetActive(true);
                callIn.gameObject.SetActive(false);
            }

            SpawnPrefabs();
            SpawnSosigs();
            SpawnObjectIDs();

            yield return new WaitForSeconds(1);
            Destroy(gameObject);
        }

        void ShakeScreen()
        {
            //Screen shake
            float ForceRadius = 10;
            float num = Vector3.Distance(base.transform.position, GM.CurrentPlayerBody.Head.position);
            float num2 = 1f;
            if (num < ForceRadius)
            {
                num2 = 0f;
            }
            else if (num < ForceRadius * 3f)
            {
                num2 = (num - ForceRadius) / (ForceRadius * 2f);
            }
            if (num2 < 1f)
            {
                SM.PowerSound(num2);
                float num3 = Mathf.Clamp(1f - num2, 0f, 1f);
                ScreenShake.TriggerShake(new ScreenShake.ShakeProfile(3.5f * num3, 0.4f), new ScreenShake.VignetteProfile(3.4f * num3, 1f, 0.35f, Color.black));
            }
        }

        public void ClearEvent(bool silent = false)
        {
            //CLEAR UI INPUTS
            currentInput = "";

            for (int i = 0; i < arrowInputs.Length; i++)
            {
                arrowInputs[i].gameObject.SetActive(false);
            }

            //Fail sound
            if (silent == false)
            {
                SM.PlayCoreSound(
                    FVRPooledAudioType.UIChirp,
                    AudEvent_Rattle,
                    transform.position);
            }


        }

        /*
        private void RattleUpdate()
        {
            if (!IsHeld || m_hand == null || selected != null)
                return;

            m_wasrattleSide = m_israttleSide;
            if (m_wasrattleSide)
            {
                m_rattleVel = RootRigidbody.velocity;
            }
            else
            {
                m_rattleVel -= RootRigidbody.GetPointVelocity(transform.TransformPoint(m_rattlePos)) * Time.deltaTime;
            }
            m_rattleVel += Vector3.up * -0.5f * Time.deltaTime;
            Vector3 vector = transform.InverseTransformDirection(m_rattleVel);
            m_rattlePos += vector * Time.deltaTime;
            float num = m_rattlePos.y;
            Vector2 vector2 = new Vector2(m_rattlePos.x, m_rattlePos.z);
            m_israttleSide = false;
            float magnitude = vector2.magnitude;
            if (magnitude > RattleRadius)
            {
                float num2 = RattleRadius - magnitude;
                vector2 = Vector3.ClampMagnitude(vector2, RattleRadius);
                num += num2 * Mathf.Sign(num);
                vector = Vector3.ProjectOnPlane(vector, new Vector3(vector2.x, 0f, vector2.y));
                m_israttleSide = true;
            }
            if (Mathf.Abs(num) > RattleHeight)
            {
                num = RattleHeight * Mathf.Sign(num);
                vector.y = 0f;
                m_israttleSide = true;
            }
            m_rattlePos = new Vector3(vector2.x, num, vector2.y);
            m_rattleVel = transform.TransformDirection(vector);
            if (m_rattleTime > 0f)
            {
                m_rattleTime -= Time.deltaTime;
            }
            if (m_israttleSide && !m_wasrattleSide && m_hand.Input.VelLinearWorld.magnitude > 0.7f && m_rattleTime <= 0f)
            {
                ClearEvent();
                m_rattleTime = 0.2f;
            }
            m_rattleLastPos = m_rattlePos;
        }
        */

        [Header("Audio")]
        public AudioEvent AudEvent_Arrow;
        public AudioEvent AudEvent_Ready;
        public AudioEvent AudEvent_ReadyLoop;
        public AudioEvent AudEvent_Beacon;
        public AudioEvent AudEvent_HellpodDrop;

        [Header("Rattle")]
        public AudioEvent AudEvent_Rattle;

        public float RattleRadius = 0.2f;
        public float RattleHeight = 0.2f;
        private Vector3 m_rattlePos = Vector3.zero;
        private Vector3 m_rattleLastPos = Vector3.zero;
        private Vector3 m_rattleVel = Vector3.zero;
        private bool m_israttleSide = false;
        private bool m_wasrattleSide = false;
        private float m_rattleTime = 0.1f;
    }
}
