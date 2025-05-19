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

        public bool antiRagdoll = false;

        public bool oneShotKill = false;

        private GUIStyle labelStyle;

        public bool godMode = false;
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

        void Start()
        {
            labelStyle = new GUIStyle();
            labelStyle.normal.textColor = Color.white;
            labelStyle.fontSize = 12;

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
            
            var player = LocalPlayer.actor;
            if (player == null) return;

            HandleSpeedHack(player);
            HandleHealthHack(player);
            HandleGodMode(player);
            HandleWeaponEnhancements(player);
            HandleRagdoll(player);
            HandleProjectiles(player);
            HandleMovementHacks(player);
            HandleSilentAim();


            if (enableSilentAim)
            {
                foreach (var proj in FindObjectsOfType<Projectile>())
                {
                    if (proj.killCredit == player)
                    {
                        ModifyProjectile(proj);
                    }
                }
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

        private void HandleGodMode(Actor player)
        {
            foreach (var col in player.GetComponentsInChildren<Collider>())
            {
                col.gameObject.layer = godMode ? 
                    LayerMask.NameToLayer("Ignore Raycast") : 
                    LayerMask.NameToLayer("Default");
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

        private void HandleRagdoll(Actor player)
        {
            if (!antiRagdoll || player == null) return;

            try
            {
                if (player.ragdoll != null)
                {
                    player.ragdoll.state = ActiveRaggy.State.Animate;
                    
                    if (player.ragdoll.ragdollObject != null && player.ragdoll.ragdollObject.activeSelf)
                    {
                        player.ragdoll.ragdollObject.SetActive(false);
                    }
                    
                    var rigidbodies = player.GetComponentsInChildren<Rigidbody>();
                    foreach (var rb in rigidbodies)
                    {
                        if (rb != null)
                        {
                            rb.isKinematic = true;
                            rb.velocity = Vector3.zero;
                            rb.angularVelocity = Vector3.zero;
                        }
                    }
                    
                    if (player.controller != null)
                    {
                        player.controller.enabled = true;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error in HandleRagdoll: {ex.Message}");
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
                    proj.configuration.damage = oneShotKill ? 9999f : defaultProjectileDamage;
                    proj.configuration.balanceDamage = oneShotKill ? 9999f : defaultProjectileBalanceDamage;
                }
            }
        }

        private void HandleMovementHacks(Actor player)
        {
            if (player.controller is FpsActorController fpsController)
            {
                HandleNoClip(fpsController);
                HandleFlyHack(fpsController);
            }
        }

        private void HandleNoClip(FpsActorController controller)
        {
            if (!enableNoClip) return;

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
                controller.transform.position += move * noClipSpeed * Time.deltaTime;
            }
        }

        private void HandleFlyHack(FpsActorController controller)
        {
            if (!enableFlyHack) return;

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
            float bestHitChance = 0f;
            targetActor = null;

            foreach (var actor in ActorManager.instance.actors)
            {
                if (actor == null || actor.dead || actor == player) continue;
                if (teamCheck && actor.team == player.team) continue;

                float distance = Vector3.Distance(player.transform.position, actor.transform.position);
                
                RaycastHit hit;
                Vector3 targetPos = actor.transform.position + Vector3.up;
                Vector3 directionToTarget = (targetPos - player.transform.position).normalized;
                
                if (Physics.Raycast(player.transform.position, directionToTarget, out hit))
                {
                    Hitbox hitbox = hit.collider.GetComponent<Hitbox>();
                    if (hitbox != null && hitbox.parent == actor)
                    {

                        float angleToTarget = Vector3.Angle(player.transform.forward, directionToTarget);
                        float hitChance = (1.0f - (distance / maxDistance)) * (1.0f - (angleToTarget / 180f));


                        if (hitChance > bestHitChance)
                        {
                            bestHitChance = hitChance;
                            closestDistance = distance;
                            targetActor = actor;
                        }

                        else if (Mathf.Abs(hitChance - bestHitChance) < 0.1f && distance < closestDistance)
                        {
                            closestDistance = distance;
                            targetActor = actor;
                        }
                    }
                }
            }
        }

        private void ModifyProjectile(Projectile proj)
        {
            if (!enableSilentAim || targetActor == null || proj == null) return;

            try
            {
                var player = LocalPlayer.actor;
                if (player == null || proj.killCredit != player) return;

                Vector3 targetPos = targetActor.transform.position + Vector3.up;
                Vector3 directionToTarget = (targetPos - proj.transform.position).normalized;
                
                if (proj.configuration != null)
                {

                    proj.configuration.damage = 150f;
                    proj.configuration.balanceDamage = 150f;

                    float projectileSpeed = proj.configuration.speed;
                    if (projectileSpeed > 0)
                    {
                        Vector3 targetVelocity = targetActor.Velocity();
                        float timeToTarget = Vector3.Distance(targetPos, proj.transform.position) / projectileSpeed;
                        targetPos += targetVelocity * timeToTarget;
                        directionToTarget = (targetPos - proj.transform.position).normalized;
                    }


                    proj.configuration.gravityMultiplier = 0f;
                    proj.configuration.speed *= 1.5f; 
                }

                // Set projectile direction and velocity
                proj.transform.forward = directionToTarget;
                if (proj.configuration != null)
                {
                    proj.velocity = directionToTarget * proj.configuration.speed;
                }
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
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

            float menuWidth = 320;
            float menuHeight = Screen.height - 40;
            Rect menuArea = new Rect(10, 20, menuWidth, menuHeight);

            GUILayout.BeginArea(menuArea, boxStyle);
            scrollPosition = GUILayout.BeginScrollView(scrollPosition, false, true);

            GUILayout.Label("RaveHack", titleStyle);
            GUILayout.Space(8);

            GUILayout.Label("General", sectionStyle);
            enableESP = GUILayout.Toggle(enableESP, "Enable ESP");
            enableSpeedHack = GUILayout.Toggle(enableSpeedHack, "Speed Hack");
            if (enableSpeedHack)
            {
                InjectedSpeedMultiplier = GUILayout.HorizontalSlider(InjectedSpeedMultiplier, 1f, 10f);
                GUILayout.Label($"Speed Multiplier: {InjectedSpeedMultiplier:F1}");
            }
            enableHealthHack = GUILayout.Toggle(enableHealthHack, "Infinite Health");
            antiRagdoll = GUILayout.Toggle(antiRagdoll, "Anti-Ragdoll");
            godMode = GUILayout.Toggle(godMode, "God Mode");
            oneShotKill = GUILayout.Toggle(oneShotKill, "One Shot Kill");
            unlimitedAmmo = GUILayout.Toggle(unlimitedAmmo, "Unlimited Ammo");
            noRecoil = GUILayout.Toggle(noRecoil, "No Recoil");
            noSpread = GUILayout.Toggle(noSpread, "No Spread");
            rapidFire = GUILayout.Toggle(rapidFire, "Rapid Fire");
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
            GUILayout.Label("ESP Settings", sectionStyle);
            showBoxESP = GUILayout.Toggle(showBoxESP, "Box ESP");
            showNameESP = GUILayout.Toggle(showNameESP, "Name ESP");
            showDistanceESP = GUILayout.Toggle(showDistanceESP, "Distance ESP");
            teamCheck = GUILayout.Toggle(teamCheck, "Team Check");
            GUILayout.Label("ESP Color:");
            espColor = RGBSlider(espColor);
            maxDistance = GUILayout.HorizontalSlider(maxDistance, 50f, 3000f);
            GUILayout.Label($"ESP Distance: {Mathf.RoundToInt(maxDistance)}m");

            GUILayout.Space(8);
            GUILayout.Label("Silent Aim Settings", sectionStyle);
            enableSilentAim = GUILayout.Toggle(enableSilentAim, "Enable Silent Aim");

            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        void DrawESP()
        {
            var localActor = ActorManager.instance?.player;
            if (localActor == null) return;

            foreach (var actor in ActorManager.instance.actors)
            {
                if (actor == null || actor.dead || actor == localActor) continue;
                if (teamCheck && actor.team == localActor.team) continue;

                float distance = Vector3.Distance(localActor.transform.position, actor.transform.position);
                if (distance > maxDistance) continue;

                Vector3 bottom = actor.transform.position;
                Vector3 top = bottom + Vector3.up * 2.0f;

                Vector3 screenBottom = Camera.main.WorldToScreenPoint(bottom);
                Vector3 screenTop = Camera.main.WorldToScreenPoint(top);

                if (screenBottom.z < 0 || screenTop.z < 0) continue;

                screenBottom.y = Screen.height - screenBottom.y;
                screenTop.y = Screen.height - screenTop.y;

                float height = screenBottom.y - screenTop.y;
                float width = height / 2;

                if (showBoxESP)
                    DrawBox(screenTop.x - width / 2, screenTop.y, width, height, espColor);

                if (showNameESP)
                {
                    var namePos = new Vector2(screenTop.x, screenTop.y - 15);
                    DrawLabel(namePos, actor.name, espColor);
                }

                if (showDistanceESP)
                {
                    var distPos = new Vector2(screenBottom.x, screenBottom.y + 5);
                    DrawLabel(distPos, $"{distance:F1}m", espColor);
                }
            }
        }

        void DrawLabel(Vector2 position, string text, Color color)
        {
            GUIStyle style = new GUIStyle(GUI.skin.label);
            style.normal.textColor = color;
            style.alignment = TextAnchor.MiddleCenter;
            style.fontSize = 12;
            GUI.Label(new Rect(position.x - 50, position.y, 100, 20), text, style);
        }

        void DrawBox(float x, float y, float w, float h, Color color)
        {
            Color old = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(new Rect(x, y + h, w, 1), Texture2D.whiteTexture); // Bottom
            GUI.DrawTexture(new Rect(x, y, 1, h), Texture2D.whiteTexture); // Left
            GUI.DrawTexture(new Rect(x + w, y, 1, h), Texture2D.whiteTexture); // Right
            GUI.color = old;
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
    }
}
