using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
using System.Collections;

namespace RaveHack
{
    public class Hacks : MonoBehaviour
    {
        private const float DEFAULT_SPEED_MULTIPLIER = 1f;
        private const float DEFAULT_HEALTH = 100f;
        private const float DEFAULT_WEAPON_COOLDOWN = 0.2f;
        private const float DEFAULT_WEAPON_RELOAD = 2f;
        private const float DEFAULT_PROJECTILE_DAMAGE = 70f;
        private const float DEFAULT_PROJECTILE_BALANCE_DAMAGE = 60f;

        public float maxDistance = 1000f;
        public bool showBoxESP = true;
        public bool showNameESP = true;
        public bool showDistanceESP = true;
        public bool enableESP = false;

        public bool teamCheck = true;

        public bool enableSpeedHack = false;
        public float InjectedSpeedMultiplier = 5f;

        public bool enableHealthHack = false;
        public float InjectedHealthHack = 50000f;

        public bool noRecoil = false;
        public bool noSpread = false;
        public bool unlimitedAmmo = false;

        public Color espColor = Color.red;

        public bool oneShotKill = false;

        private GUIStyle labelStyle;

        public bool rapidFire = false;
        private bool showMenu = true;

        private float defaultProjectileDamage;
        private float defaultProjectileBalanceDamage;
        private float defaultProjectileGravity;
        private float defaultProjectileSpeed;
        private float defaultWeaponCooldown;
        private float defaultWeaponReload;

        private float fpsUpdate = 0f;
        private int fpsFrames = 0;
        private float lastFps = 0f;

        public bool enableFlyHack = false;
        public float flySpeed = 50f;

        public bool enableNoClip = false;
        public float noClipSpeed = 50f;

        private bool initialized = false;


        public bool enableSilentAim = false;
        private Actor targetActor;

        private Vector2 scrollPosition;


        public KeyCode espKey = KeyCode.F1;
        public KeyCode speedHackKey = KeyCode.F2;
        public KeyCode healthHackKey = KeyCode.F3;
        public KeyCode oneShotKey = KeyCode.F5;
        public KeyCode noRecoilKey = KeyCode.F6;
        public KeyCode noSpreadKey = KeyCode.F7;
        public KeyCode rapidFireKey = KeyCode.F8;
        public KeyCode flyHackKey = KeyCode.F9;
        public KeyCode noClipKey = KeyCode.F10;
        public KeyCode silentAimKey = KeyCode.F11;

        private bool waitingForKey = false;
        private string currentKeyBindAction = "";


        public float silentAimFov = 90f;
        private Material circleMaterial;
        private const int CIRCLE_SEGMENTS = 64;


        public bool showHealthBarESP = true;
        public bool showSkeletonESP = true;
        public bool showHeadDotESP = true;
        public bool showTracerESP = true;


        public bool enableChams = false;
        public Color chamsColor = Color.magenta;
        private const float CHAMS_MAX_DISTANCE = 70f;
        private Dictionary<SkinnedMeshRenderer, Material> originalChamsMaterials = new Dictionary<SkinnedMeshRenderer, Material>();

        public float silentAimMaxDistance = 500f;

        private float silentAimUpdateTimer = 0f;
        private float silentAimUpdateInterval = 0.15f;
        
        private bool lastChamsState = false;
        private int lastChamsActorCount = 0;

        private int lastWeaponAmmo = -1;


        private float chamsUpdateTimer = 0f;
        private float chamsUpdateInterval = 1f;
        private HashSet<Projectile> lastProjectiles = new HashSet<Projectile>();

        public bool enableCustomCrosshair = false;
        public Color crosshairColor = Color.green;
        public int crosshairType = 0; // 0 dot 1 cross 2 circle
        public bool rainbowChams = false;
        public bool rainbowESP = false;

        public bool enemiesCantShoot = false;


        public float chamsDistance = 70f;

        void Start()
        {
            labelStyle = new GUIStyle();
            labelStyle.normal.textColor = Color.white;
            labelStyle.fontSize = 12;


            CreateCircleMaterial();

            SaveDefaultValues();
            initialized = true;
        }

        private void SaveDefaultValues()
        {
            var proj = FindObjectOfType<Projectile>();
            if (proj != null)
            {
                defaultProjectileDamage = proj.configuration.damage;
                defaultProjectileBalanceDamage = proj.configuration.balanceDamage;
                defaultProjectileGravity = proj.configuration.gravityMultiplier;
                defaultProjectileSpeed = proj.configuration.speed;
            }

            var player = LocalPlayer.actor;
            if (player != null && player.weapons != null && player.activeWeapon != null)
            {
                var weapon = player.activeWeapon;
                defaultWeaponCooldown = weapon.configuration.cooldown;
                defaultWeaponReload = weapon.configuration.reloadTime;
            }
        }

        void Update()
        {
            if (!initialized)
            {
                SaveDefaultValues();
                initialized = true;
            }

            UpdateFPS();
            HandleMenuToggle();
            HandleKeybinds();
            
            var player = LocalPlayer.actor;
            if (player == null) return;

            HandleSpeedHack(player);
            HandleHealthHack(player);
            HandleWeaponEnhancements(player);
            HandleProjectiles(player);
            HandleMovementHacks(player);
            if (enemiesCantShoot) HandleTakeEnemyWeapons();


            if (enableSilentAim && player.activeWeapon != null)
            {
                var allProjectiles = FindObjectsOfType<Projectile>();
                var newProjectiles = new List<Projectile>();
                foreach (var proj in allProjectiles)
                {
                    if (!lastProjectiles.Contains(proj) && proj.killCredit == player && proj.sourceWeapon == player.activeWeapon)
                    {
                        newProjectiles.Add(proj);
                    }
                }
                foreach (var proj in newProjectiles)
                {
                    var target = FindSilentAimTargetFromProjectile(proj);
                    if (target != null)
                        ModifyProjectile(proj, target);
                }
                lastProjectiles = new HashSet<Projectile>(allProjectiles);
            }
            else
            {
                lastProjectiles.Clear();
            }


            chamsUpdateTimer += Time.deltaTime;
            if (enableChams && chamsUpdateTimer >= chamsUpdateInterval)
            {
                HandleChams();
                chamsUpdateTimer = 0f;
            }
        }

        private void UpdateFPS()
        {
            fpsFrames++;
            fpsUpdate += Time.unscaledDeltaTime;
            if (fpsUpdate > 0.5f)
            {
                lastFps = fpsFrames / fpsUpdate;
                fpsFrames = 0;
                fpsUpdate = 0f;
            }
        }

        private void HandleMenuToggle()
        {
            if (Input.GetKeyDown(KeyCode.Insert))
            {
                showMenu = !showMenu;
                if (FpsActorController.instance != null)
                    FpsActorController.instance.unlockCursorRavenscriptOverride = showMenu;
                Cursor.visible = showMenu;
                Cursor.lockState = showMenu ? CursorLockMode.None : CursorLockMode.Locked;
            }
        }

        private void HandleKeybinds()
        {
            if (!waitingForKey) 
            {
                if (Input.GetKeyDown(espKey)) enableESP = !enableESP;
                if (Input.GetKeyDown(speedHackKey)) enableSpeedHack = !enableSpeedHack;
                if (Input.GetKeyDown(healthHackKey)) enableHealthHack = !enableHealthHack;
                if (Input.GetKeyDown(oneShotKey)) oneShotKill = !oneShotKill;
                if (Input.GetKeyDown(noRecoilKey)) noRecoil = !noRecoil;
                if (Input.GetKeyDown(noSpreadKey)) noSpread = !noSpread;
                if (Input.GetKeyDown(rapidFireKey)) rapidFire = !rapidFire;
                if (Input.GetKeyDown(flyHackKey)) enableFlyHack = !enableFlyHack;
                if (Input.GetKeyDown(noClipKey)) enableNoClip = !enableNoClip;
                if (Input.GetKeyDown(silentAimKey)) enableSilentAim = !enableSilentAim;
            }
        }

        private void HandleSpeedHack(Actor player)
        {
            if (player.controller != null)
            {
                player.speedMultiplier = enableSpeedHack ? InjectedSpeedMultiplier : DEFAULT_SPEED_MULTIPLIER;
            }
        }

        private void HandleHealthHack(Actor player)
        {
            if (enableHealthHack)
            {
                player.health = InjectedHealthHack;
            }
        }

        private void HandleWeaponEnhancements(Actor player)
        {
            if (player == null || player.weapons == null || player.activeWeapon == null) return;

            var weapon = player.activeWeapon;
            var config = weapon.configuration;

            if (rapidFire)
            {
                config.cooldown = 0.00f;
            }

            if (unlimitedAmmo)
            {
                config.ammo = 999;
                config.spareAmmo = 999;
                config.resupplyNumber = 999;
                config.dropAmmoWhenReloading = false;
                config.useMaxAmmoPerReload = false;
                config.forceAutoReload = true;
                weapon.ammo = 999;
                weapon.spareAmmo = 999;
            }

            if (noRecoil)
            {
                config.kickback = 0f;
                config.randomKick = 0f;
                config.snapMagnitude = 0f;
                config.snapDuration = 0f;
                config.snapFrequency = 0f;
            }

            if (noSpread)
            {
                config.spread = 0f;
                config.followupSpreadGain = 0f;
                config.followupMaxSpreadHip = 0f;
                config.followupMaxSpreadAim = 0f;
                config.followupSpreadStayTime = 0f;
                config.followupSpreadDissipateTime = 0f;
            }
        }

        private void HandleProjectiles(Actor player)
        {
            if (player.activeWeapon == null) return;

            var weapon = player.activeWeapon;
            foreach (var proj in FindObjectsOfType<Projectile>())
            {
                if (proj.sourceWeapon == weapon)
                {
                    if (oneShotKill)
                    {
                        proj.configuration.damage = 9999f;
                        proj.configuration.balanceDamage = 9999f;
                    }
                    else
                    {
                        if (proj.configuration.damage <= 0f)
                            proj.configuration.damage = defaultProjectileDamage > 0 ? defaultProjectileDamage : 70f;
                        if (proj.configuration.balanceDamage <= 0f)
                            proj.configuration.balanceDamage = defaultProjectileBalanceDamage > 0 ? defaultProjectileBalanceDamage : 60f;
                    }
                }
            }
        }

        private void HandleMovementHacks(Actor player)
        {
            if (player.controller is FpsActorController fpsController)
            {
                bool isProne = false;
                var proneField = player.GetType().GetField("prone", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                if (proneField != null)
                {
                    isProne = (bool)proneField.GetValue(player);
                }
                if (!isProne)
                {
                    HandleNoClip(fpsController);
                    HandleFlyHack(fpsController);
                }
                if (enableNoClip || enableFlyHack)
                {
                    if (Input.GetKeyDown(KeyCode.Z) || Input.GetKeyDown(KeyCode.C))
                    {
                    }
                }
            }
        }

        private void HandleNoClip(FpsActorController controller)
        {
            if (!enableNoClip) return;

            foreach (var col in controller.GetComponentsInChildren<Collider>())
                col.enabled = false;

            Vector3 move = Vector3.zero;
            if (Input.GetKey(KeyCode.W)) move += controller.transform.forward;
            if (Input.GetKey(KeyCode.S)) move -= controller.transform.forward;
            if (Input.GetKey(KeyCode.A)) move -= controller.transform.right;
            if (Input.GetKey(KeyCode.D)) move += controller.transform.right;
            if (Input.GetKey(KeyCode.Space)) move += Vector3.up * 0.5f;
            if (Input.GetKey(KeyCode.LeftControl)) move += Vector3.down * 0.5f;
            if (move != Vector3.zero)
            {
                move.Normalize();
                controller.transform.position += move * noClipSpeed * Time.deltaTime;
            }
        }

        private void HandleFlyHack(FpsActorController controller)
        {
            if (!enableFlyHack) return;

            foreach (var col in controller.GetComponentsInChildren<Collider>())
                col.enabled = true;

            Vector3 move = Vector3.zero;
            if (Input.GetKey(KeyCode.W)) move += Camera.main.transform.forward;
            if (Input.GetKey(KeyCode.S)) move -= Camera.main.transform.forward;
            if (Input.GetKey(KeyCode.A)) move -= Camera.main.transform.right;
            if (Input.GetKey(KeyCode.D)) move += Camera.main.transform.right;
            if (Input.GetKey(KeyCode.Space)) move += Vector3.up;
            if (Input.GetKey(KeyCode.LeftControl)) move += Vector3.down;
            if (move != Vector3.zero)
            {
                move.Normalize();
                controller.transform.position += move * flySpeed * Time.deltaTime;
            }
        }

        private void HandleSilentAim()
        {
            if (!enableSilentAim) return;

            var player = LocalPlayer.actor;
            if (player == null || player.activeWeapon == null) return;

            float closestDistance = float.MaxValue;
            float bestScore = float.MinValue;
            targetActor = null;

            Vector3 cameraPos = Camera.main.transform.position;
            Vector3 cameraForward = Camera.main.transform.forward;

            foreach (var actor in ActorManager.instance.actors)
            {
                if (actor == null || actor.dead || actor == player) continue;
                if (teamCheck && actor.team == player.team) continue;

                Vector3 targetPos = actor.transform.position + Vector3.up * 1.1f;
                Vector3 dirToTarget = (targetPos - cameraPos).normalized;
                float angle = Vector3.Angle(cameraForward, dirToTarget);
                if (angle > silentAimFov / 2f) continue;

                float distance = Vector3.Distance(cameraPos, targetPos);
                if (distance > silentAimMaxDistance) continue;

                float score = (1.0f - (angle / (silentAimFov / 2f))) * (1.0f - (distance / silentAimMaxDistance));
                if (score > bestScore)
                {
                    bestScore = score;
                    closestDistance = distance;
                    targetActor = actor;
                }
            }
        }

        private void ModifyProjectile(Projectile proj, Actor target)
        {
            if (!enableSilentAim || target == null || proj == null) return;

            try
            {
                var player = LocalPlayer.actor;
                if (player == null || proj.killCredit != player) return;

                Vector3 targetPos = target.transform.position + Vector3.up * 1.1f;
                Vector3 projPos = proj.transform.position;
                Vector3 toTarget = targetPos - projPos;

                if (proj.configuration != null)
                {
                    float projectileSpeed = proj.configuration.speed;
                    if (projectileSpeed > 0)
                    {
                        Vector3 targetVelocity = target.Velocity();
                        float timeToTarget = toTarget.magnitude / projectileSpeed;
                        targetPos += targetVelocity * timeToTarget;
                        toTarget = targetPos - projPos;
                    }

                    proj.configuration.gravityMultiplier = 0f;
                    proj.configuration.speed = Mathf.Max(proj.configuration.speed, 50f);
                }

                Vector3 direction = toTarget.normalized;
                proj.transform.forward = direction;
                if (proj.configuration != null)
                {
                    proj.velocity = direction * proj.configuration.speed;
                }
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        private void CreateCircleMaterial()
        {
            if (circleMaterial != null) return;

            var shader = Shader.Find("Hidden/Internal-Colored");
            circleMaterial = new Material(shader);
            circleMaterial.hideFlags = HideFlags.HideAndDontSave;
            circleMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            circleMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            circleMaterial.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
            circleMaterial.SetInt("_ZWrite", 0);
        }

        void OnRenderObject()
        {
            if (!enableSilentAim) return;

            CreateCircleMaterial();
            circleMaterial.SetPass(0);

            GL.PushMatrix();
            GL.LoadPixelMatrix();

            float radius = Screen.height * Mathf.Tan(silentAimFov * 0.5f * Mathf.Deg2Rad);
            Vector2 center = new Vector2(Screen.width / 2f, Screen.height / 2f);


            GL.Begin(GL.LINES);
            GL.Color(new Color(1f, 1f, 1f, 0.5f));
            
            for (int i = 0; i < CIRCLE_SEGMENTS; i++)
            {
                float angle1 = (i / (float)CIRCLE_SEGMENTS) * 2 * Mathf.PI;
                float angle2 = ((i + 1) / (float)CIRCLE_SEGMENTS) * 2 * Mathf.PI;

                Vector2 point1 = new Vector2(Mathf.Cos(angle1), Mathf.Sin(angle1));
                Vector2 point2 = new Vector2(Mathf.Cos(angle2), Mathf.Sin(angle2));

                GL.Vertex(center + point1 * radius);
                GL.Vertex(center + point2 * radius);
            }

            GL.End();
            GL.PopMatrix();
        }

        void OnGUI()
        {
            GUIStyle watermarkStyle = new GUIStyle(GUI.skin.label);
            watermarkStyle.fontSize = 18;
            watermarkStyle.fontStyle = FontStyle.Bold;
            watermarkStyle.normal.textColor = Color.white;
            watermarkStyle.alignment = TextAnchor.UpperCenter;
            GUI.Label(new Rect(Screen.width / 2 - 200, 10, 400, 30), $"RaveHack v1 Menu: INSERT   FPS: {(int)lastFps}", watermarkStyle);

            if (enableESP) DrawESP();

            if (showMenu)
            {
                DrawGUI();
            }

            if (enableCustomCrosshair)
                DrawCustomCrosshair();
        }

        void DrawGUI()
        {
            GUIStyle boxStyle = new GUIStyle(GUI.skin.box);
            boxStyle.normal.background = MakeTex(2, 2, new Color(0.13f, 0.13f, 0.13f, 0.95f));
            boxStyle.border = new RectOffset(10, 10, 10, 10);
            boxStyle.padding = new RectOffset(12, 12, 12, 12);

            GUIStyle titleStyle = new GUIStyle(GUI.skin.label);
            titleStyle.fontSize = 20;
            titleStyle.fontStyle = FontStyle.Bold;
            titleStyle.normal.textColor = new Color(0.9f, 0.3f, 0.3f);
            titleStyle.alignment = TextAnchor.MiddleCenter;

            GUIStyle sectionStyle = new GUIStyle(GUI.skin.label);
            sectionStyle.fontSize = 14;
            sectionStyle.fontStyle = FontStyle.Bold;
            sectionStyle.normal.textColor = new Color(0.7f, 0.7f, 1f);
            sectionStyle.margin = new RectOffset(0, 0, 8, 4);

            float menuWidth = 340;
            float menuHeight = Screen.height - 40;
            Rect menuArea = new Rect(10, 20, menuWidth, menuHeight);

            GUILayout.BeginArea(menuArea, boxStyle);
            scrollPosition = GUILayout.BeginScrollView(scrollPosition, false, true);

            GUILayout.Label("RaveHack", titleStyle);
            GUILayout.Space(8);

            GUILayout.Label("General", sectionStyle);
            enemiesCantShoot = GUILayout.Toggle(enemiesCantShoot, "Enemies Can't Shoot");
            GUILayout.Space(8);

            GUILayout.Label("Visuals", sectionStyle);
            enableESP = GUILayout.Toggle(enableESP, "Enable ESP");
            rainbowESP = GUILayout.Toggle(rainbowESP, "Rainbow ESP");
            showBoxESP = GUILayout.Toggle(showBoxESP, "Box ESP");
            showNameESP = GUILayout.Toggle(showNameESP, "Name ESP");
            showDistanceESP = GUILayout.Toggle(showDistanceESP, "Distance ESP");
            showHealthBarESP = GUILayout.Toggle(showHealthBarESP, "Health Bar ESP");
            showSkeletonESP = GUILayout.Toggle(showSkeletonESP, "Skeleton ESP");
            showHeadDotESP = GUILayout.Toggle(showHeadDotESP, "Head Dot ESP");
            showTracerESP = GUILayout.Toggle(showTracerESP, "Tracer ESP");
            teamCheck = GUILayout.Toggle(teamCheck, "Team Check");
            GUILayout.Label("ESP Color:");
            espColor = RGBSlider(espColor);
            maxDistance = GUILayout.HorizontalSlider(maxDistance, 50f, 3000f);
            GUILayout.Label($"ESP Distance: {Mathf.RoundToInt(maxDistance)}m");
            GUILayout.Space(8);

            enableChams = GUILayout.Toggle(enableChams, "Enable Chams");
            rainbowChams = GUILayout.Toggle(rainbowChams, "Rainbow Chams");
            GUILayout.Label("Chams Color:");
            chamsColor = RGBSlider(chamsColor);
            chamsDistance = GUILayout.HorizontalSlider(chamsDistance, 10f, 300f);
            GUILayout.Label($"Chams Distance: {Mathf.RoundToInt(chamsDistance)}m");
            GUILayout.Space(8);

            enableCustomCrosshair = GUILayout.Toggle(enableCustomCrosshair, "Custom Crosshair");
            if (enableCustomCrosshair)
            {
                GUILayout.Label("Crosshair Color:");
                crosshairColor = RGBSlider(crosshairColor);
                GUILayout.Label("Crosshair Type:");
                crosshairType = GUILayout.SelectionGrid(crosshairType, new string[] { "Dot", "Cross", "Circle" }, 3);
            }
            GUILayout.Space(8);

            GUILayout.Label("Weapon", sectionStyle);
            oneShotKill = GUILayout.Toggle(oneShotKill, "One Shot Kill");
            unlimitedAmmo = GUILayout.Toggle(unlimitedAmmo, "Unlimited Ammo");
            noRecoil = GUILayout.Toggle(noRecoil, "No Recoil");
            noSpread = GUILayout.Toggle(noSpread, "No Spread");
            rapidFire = GUILayout.Toggle(rapidFire, "Rapid Fire");
            GUILayout.Space(8);

            GUILayout.Label("Movement", sectionStyle);
            enableSpeedHack = GUILayout.Toggle(enableSpeedHack, "Speed Hack");
            if (enableSpeedHack)
            {
                InjectedSpeedMultiplier = GUILayout.HorizontalSlider(InjectedSpeedMultiplier, 1f, 10f);
                GUILayout.Label($"Speed Multiplier: {InjectedSpeedMultiplier:F1}");
            }
            enableFlyHack = GUILayout.Toggle(enableFlyHack, "Fly");
            if (enableFlyHack)
            {
                flySpeed = GUILayout.HorizontalSlider(flySpeed, 5f, 100f);
                GUILayout.Label($"Fly Speed: {flySpeed:F1}");
            }
            enableNoClip = GUILayout.Toggle(enableNoClip, "No-Clip");
            if (enableNoClip)
            {
                noClipSpeed = GUILayout.HorizontalSlider(noClipSpeed, 5f, 100f);
                GUILayout.Label($"No-Clip Speed: {noClipSpeed:F1}");
            }
            GUILayout.Space(8);

            GUILayout.Label("Silent Aim", sectionStyle);
            enableSilentAim = GUILayout.Toggle(enableSilentAim, $"Enable Silent Aim [{silentAimKey}]");
            if (enableSilentAim)
            {
                silentAimFov = GUILayout.HorizontalSlider(silentAimFov, 10f, 180f);
                GUILayout.Label($"FOV: {silentAimFov:F1}°");
                silentAimMaxDistance = GUILayout.HorizontalSlider(silentAimMaxDistance, 50f, 2000f);
                GUILayout.Label($"Silent Aim Distance: {Mathf.RoundToInt(silentAimMaxDistance)}m");
            }
            GUILayout.Space(8);

            GUILayout.Label("Keybinds", sectionStyle);
            if (waitingForKey)
            {
                GUIStyle waitingStyle = new GUIStyle(GUI.skin.label);
                waitingStyle.normal.textColor = Color.yellow;
                waitingStyle.fontSize = 14;
                waitingStyle.alignment = TextAnchor.MiddleCenter;
                GUILayout.Label("Press any key for: " + currentKeyBindAction, waitingStyle);
            }
            else
            {
                DrawKeybindButton("ESP", espKey, k => espKey = k);
                DrawKeybindButton("Speed Hack", speedHackKey, k => speedHackKey = k);
                DrawKeybindButton("One Shot", oneShotKey, k => oneShotKey = k);
                DrawKeybindButton("No Recoil", noRecoilKey, k => noRecoilKey = k);
                DrawKeybindButton("No Spread", noSpreadKey, k => noSpreadKey = k);
                DrawKeybindButton("Rapid Fire", rapidFireKey, k => rapidFireKey = k);
                DrawKeybindButton("Fly Hack", flyHackKey, k => flyHackKey = k);
                DrawKeybindButton("No-Clip", noClipKey, k => noClipKey = k);
                DrawKeybindButton("Silent Aim", silentAimKey, k => silentAimKey = k);
            }

            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private void DrawKeybindButton(string name, KeyCode key, Action<KeyCode> setKey)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(name, GUILayout.Width(100));
            if (GUILayout.Button(key.ToString(), GUILayout.Width(100)))
            {
                waitingForKey = true;
                currentKeyBindAction = name;
                StartCoroutine(WaitForKey(result => {
                    setKey(result);
                    waitingForKey = false;
                    currentKeyBindAction = "";
                }));
            }
            GUILayout.EndHorizontal();
        }

        private IEnumerator WaitForKey(System.Action<KeyCode> callback)
        {
            yield return new WaitForEndOfFrame();
            
            while (!Input.anyKeyDown)
                yield return null;

            foreach (KeyCode key in System.Enum.GetValues(typeof(KeyCode)))
            {
                if (Input.GetKeyDown(key))
                {
                 
                    if (key != KeyCode.Insert)
                    {
                        callback(key);
                    }
                    break;
                }
            }
        }

        void DrawESP()
        {
            var localActor = ActorManager.instance?.player;
            if (localActor == null) return;

            Vector2 screenCenter = new Vector2(Screen.width / 2f, Screen.height);

            foreach (var actor in ActorManager.instance.actors)
            {
                if (actor == null || actor.dead || actor == localActor) continue;
                if (teamCheck && actor.team == localActor.team) continue;

                float distance = Vector3.Distance(localActor.transform.position, actor.transform.position);
                if (distance > maxDistance) continue;

                Color color = (actor.team == localActor.team) ? Color.cyan : (rainbowESP ? GetRainbowColor(0.5f) : espColor);

                Vector3 bottom = actor.transform.position;
                Vector3 top = bottom + Vector3.up * 2.0f;
                Vector3 head = bottom + Vector3.up * 1.8f;
                Vector3 chest = bottom + Vector3.up * 1.1f;

                Vector3 screenBottom = Camera.main.WorldToScreenPoint(bottom);
                Vector3 screenTop = Camera.main.WorldToScreenPoint(top);
                Vector3 screenHead = Camera.main.WorldToScreenPoint(head);
                Vector3 screenChest = Camera.main.WorldToScreenPoint(chest);

                if (screenBottom.z < 0 || screenTop.z < 0 || screenHead.z < 0) continue;

                screenBottom.y = Screen.height - screenBottom.y;
                screenTop.y = Screen.height - screenTop.y;
                screenHead.y = Screen.height - screenHead.y;
                screenChest.y = Screen.height - screenChest.y;

                float height = screenBottom.y - screenTop.y;
                float width = height / 2;

                if (showTracerESP)
                    DrawLine(new Vector2(Screen.width / 2f, Screen.height - 10), new Vector2(screenBottom.x, screenBottom.y), color, 2f);

                if (showBoxESP)
                    DrawBox(screenTop.x - width / 2, screenTop.y, width, height, color);

                if (showHealthBarESP)
                {
                    float health = Mathf.Clamp(actor.health, 0, 100);
                    float healthBarHeight = height;
                    float healthBarWidth = 5f;
                    float healthBarX = screenTop.x - width / 2 - 8f;
                    float healthBarY = screenTop.y;
                    DrawHealthBar(healthBarX, healthBarY, healthBarWidth, healthBarHeight, health / 100f);
                }

                if (showSkeletonESP)
                {
                    DrawLine(new Vector2(screenHead.x, screenHead.y), new Vector2(screenChest.x, screenChest.y), color, 2f);
                    DrawLine(new Vector2(screenChest.x, screenChest.y), new Vector2(screenBottom.x, screenBottom.y), color, 2f);
                }

                if (showHeadDotESP)
                    DrawCircle(new Vector2(screenHead.x, screenHead.y), 6f, color, 1.5f);

                int fontSize = Mathf.Clamp((int)(32 - distance / 10f), 10, 22);

                if (showNameESP)
                {
                    var namePos = new Vector2(screenTop.x, screenTop.y - 18);
                    DrawLabel(namePos, actor.name, color, fontSize);
                }

                if (showDistanceESP)
                {
                    var distPos = new Vector2(screenBottom.x, screenBottom.y + 8);
                    DrawLabel(distPos, $"{distance:F1}m", color, fontSize);
                }
            }
        }

        void DrawLabel(Vector2 position, string text, Color color, int fontSize = 12)
        {
            GUIStyle style = new GUIStyle(GUI.skin.label);
            style.normal.textColor = color;
            style.alignment = TextAnchor.MiddleCenter;
            style.fontSize = fontSize;
            GUI.Label(new Rect(position.x - 50, position.y, 100, 20), text, style);
        }

        void DrawBox(float x, float y, float w, float h, Color color)
        {
            Color old = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(new Rect(x, y + h, w, 1), Texture2D.whiteTexture); // Bottom
            GUI.DrawTexture(new Rect(x, y, 1, h), Texture2D.whiteTexture); // Left
            GUI.DrawTexture(new Rect(x + w, y, 1, h), Texture2D.whiteTexture); // Right
            GUI.DrawTexture(new Rect(x, y, w, 1), Texture2D.whiteTexture); // Top
            GUI.color = old;
        }

        void DrawHealthBar(float x, float y, float w, float h, float percent)
        {
            Color old = GUI.color;
            GUI.color = Color.black;
            GUI.DrawTexture(new Rect(x, y, w, h), Texture2D.whiteTexture);
            GUI.color = Color.green;
            GUI.DrawTexture(new Rect(x, y + h * (1 - percent), w, h * percent), Texture2D.whiteTexture);
            GUI.color = old;
        }

        void DrawLine(Vector2 p1, Vector2 p2, Color color, float thickness = 1f)
        {
            Color old = GUI.color;
            Matrix4x4 matrix = GUI.matrix;
            GUI.color = color;
            float angle = Mathf.Atan2(p2.y - p1.y, p2.x - p1.x) * Mathf.Rad2Deg;
            float length = Vector2.Distance(p1, p2);
            GUIUtility.RotateAroundPivot(angle, p1);
            GUI.DrawTexture(new Rect(p1.x, p1.y, length, thickness), Texture2D.whiteTexture);
            GUI.matrix = matrix;
            GUI.color = old;
        }

        void DrawCircle(Vector2 center, float radius, Color color, float thickness = 1f)
        {
            int segments = 24;
            Vector2 prev = center + new Vector2(Mathf.Cos(0), Mathf.Sin(0)) * radius;
            for (int i = 1; i <= segments; i++)
            {
                float theta = (i / (float)segments) * 2 * Mathf.PI;
                Vector2 next = center + new Vector2(Mathf.Cos(theta), Mathf.Sin(theta)) * radius;
                DrawLine(prev, next, color, thickness);
                prev = next;
            }
        }

        Color RGBSlider(Color c)
        {
            c.r = GUILayout.HorizontalSlider(c.r, 0, 1);
            c.g = GUILayout.HorizontalSlider(c.g, 0, 1);
            c.b = GUILayout.HorizontalSlider(c.b, 0, 1);
            return c;
        }

        Texture2D MakeTex(int width, int height, Color col)
        {
            Color[] pix = new Color[width * height];
            for (int i = 0; i < pix.Length; ++i)
                pix[i] = col;
            Texture2D result = new Texture2D(width, height);
            result.SetPixels(pix);
            result.Apply();
            return result;
        }

        private void HandleChams()
        {
            var localActor = ActorManager.instance?.player;
            Color chamsCol = rainbowChams ? GetRainbowColor(0.5f) : chamsColor;
            foreach (var actor in ActorManager.instance.actors)
            {
                if (actor == null || actor.dead) continue;
                var skinnedRenderers = actor.GetComponentsInChildren<SkinnedMeshRenderer>();
                if (skinnedRenderers == null || skinnedRenderers.Length == 0) continue;

                float distance = localActor != null ? Vector3.Distance(localActor.transform.position, actor.transform.position) : 0f;
                bool isEnemy = localActor != null && actor.team != localActor.team;
                bool inDistance = distance <= chamsDistance;

                foreach (var skinned in skinnedRenderers)
                {
                    if (!originalChamsMaterials.ContainsKey(skinned))
                        originalChamsMaterials[skinned] = skinned.sharedMaterial;

                    if (enableChams && isEnemy && inDistance)
                    {
                        var shader = Shader.Find("Hidden/Internal-Colored");
                        if (shader != null)
                        {
                            skinned.material.shader = shader;
                            skinned.material.color = chamsCol;
                            skinned.material.SetInt("_ZTest", (int)UnityEngine.Rendering.CompareFunction.Always);
                            skinned.material.renderQueue = 3000;
                        }
                    }
                    else
                    {
                        if (originalChamsMaterials.ContainsKey(skinned))
                            skinned.sharedMaterial = originalChamsMaterials[skinned];
                    }
                }
            }
        }

        private Actor FindSilentAimTargetFromProjectile(Projectile proj)
        {
            var player = LocalPlayer.actor;
            if (player == null || player.activeWeapon == null) return null;

            float bestScore = float.MinValue;
            Actor bestTarget = null;

            Vector3 projPos = proj.transform.position;
            Vector3 projForward = proj.transform.forward;

            foreach (var actor in ActorManager.instance.actors)
            {
                if (actor == null || actor.dead || actor == player) continue;
                if (teamCheck && actor.team == player.team) continue;

                Vector3 targetPos = actor.transform.position + Vector3.up * 1.1f;
                Vector3 dirToTarget = (targetPos - projPos).normalized;
                float angle = Vector3.Angle(projForward, dirToTarget);
                if (angle > silentAimFov / 2f) continue;

                float distance = Vector3.Distance(projPos, targetPos);
                if (distance > silentAimMaxDistance) continue;

                float score = (1.0f - (angle / (silentAimFov / 2f))) * (1.0f - (distance / silentAimMaxDistance));
                if (score > bestScore)
                {
                    bestScore = score;
                    bestTarget = actor;
                }
            }
            return bestTarget;
        }

        void LateUpdate()
        {
            var player = LocalPlayer.actor;
            if (player != null)
            {
                var proneField = player.GetType().GetField("prone", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                if (proneField != null)
                {
                    proneField.SetValue(player, false);
                }
            }
        }

        void DrawCustomCrosshair()
        {
            Vector2 center = new Vector2(Screen.width / 2f, Screen.height / 2f);
            Color old = GUI.color;
            GUI.color = crosshairColor;
            if (crosshairType == 0) // dot
                GUI.DrawTexture(new Rect(center.x - 3, center.y - 3, 6, 6), Texture2D.whiteTexture);
            else if (crosshairType == 1) // cross
            {
                GUI.DrawTexture(new Rect(center.x - 10, center.y - 1, 20, 2), Texture2D.whiteTexture);
                GUI.DrawTexture(new Rect(center.x - 1, center.y - 10, 2, 20), Texture2D.whiteTexture);
            }
            else if (crosshairType == 2) // circle
                DrawCircle(center, 12, crosshairColor, 2f);
            GUI.color = old;
        }

        private Color GetRainbowColor(float speed = 1f)
        {
            float t = Time.time * speed;
            return Color.HSVToRGB((t % 1f), 1f, 1f);
        }

        private void HandleTakeEnemyWeapons()
        {
            foreach (var actor in ActorManager.instance.actors)
            {
                if (actor == null || actor.dead || actor == LocalPlayer.actor || actor.team == LocalPlayer.actor.team) continue;
                var weapon = actor.activeWeapon;
                if (weapon != null)
                {
                    weapon.ammo = 0;
                    weapon.spareAmmo = 0;
                    var config = weapon.configuration;
                    if (config != null)
                    {
                        config.ammo = 0;
                        config.spareAmmo = 0;
                        config.cooldown = 9999f;
                        var canFireField = config.GetType().GetField("canFire", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                        if (canFireField != null) canFireField.SetValue(config, false);
                        var canShootField = config.GetType().GetField("canShoot", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                        if (canShootField != null) canShootField.SetValue(config, false);
                    }
                }
            }
        }
    }
}

