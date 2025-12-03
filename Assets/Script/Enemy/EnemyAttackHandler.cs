﻿using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Script.UI;

namespace Script.Enemy
{
    // Gắn script vào Enemy, assign UI references trong Inspector
    // Script tự động check attack state trong Update(), không cần Animation Event
    public class EnemyAttackHandler : MonoBehaviour
    {
        [Header("Attack Settings")]
        [SerializeField] private float attackRange = 3f;
        [SerializeField] private Transform attackPoint;
        
        [Header("UI References")]
        [SerializeField] private GameObject youreDeadPanel;
        [SerializeField] private Button restartButton;
        [SerializeField] private TMP_Text deathText;
        
        [Header("References")]
        [SerializeField] private Transform player;
        [SerializeField] private Animator enemyAnimator;
        
        [Header("Checkpoint")]
        [SerializeField] private Transform playerCheckpoint;
        [SerializeField] private Transform enemyCheckpoint;
        
        private bool _hasKilledPlayer;
        private bool _isAttacking;
        private bool _hasCheckedThisAttack;
        private int _isAttackHash;
        private bool _hasIsAttackParameter;
    
    void Start()
    {
        _hasKilledPlayer = false;
        _isAttacking = false;
        _hasCheckedThisAttack = false;
        
        if (youreDeadPanel == null)
        {
            Debug.LogError("EnemyAttackHandler: You're Dead Panel is not assigned!");
        }
        else
        {
            youreDeadPanel.SetActive(false);
        }
        
        if (restartButton != null)
        {
            restartButton.onClick.RemoveAllListeners();
            restartButton.onClick.AddListener(OnRestartButtonClicked);
        }
        
        if (enemyAnimator == null)
        {
            enemyAnimator = GetComponent<Animator>();
        }
        
        if (enemyAnimator != null)
        {
            _isAttackHash = Animator.StringToHash("IsAttack");
            
            foreach (AnimatorControllerParameter param in enemyAnimator.parameters)
            {
                if (param.name == "IsAttack")
                {
                    _hasIsAttackParameter = true;
                    enemyAnimator.SetBool(_isAttackHash, false);
                    break;
                }
            }
        }
        
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                player = playerObj.transform;
            }
        }
        
        if (attackPoint == null)
        {
            attackPoint = transform;
        }
    }
    
    void Update()
    {
        if (_hasKilledPlayer) return;
        if (!StarterAssets.StarterAssetsInputs.IsGameActive()) return;
        if (enemyAnimator == null) return;
        
        AnimatorStateInfo stateInfo = enemyAnimator.GetCurrentAnimatorStateInfo(0);
        
        bool isAttackParameter = false;
        if (_hasIsAttackParameter)
        {
            isAttackParameter = enemyAnimator.GetBool(_isAttackHash);
        }
        
        bool isInAttackState = isAttackParameter ||
                               stateInfo.IsName("Attack") || 
                               stateInfo.IsName("Zombie Attack") ||
                               stateInfo.IsName("ZombieAttack") ||
                               stateInfo.IsName("attack") ||
                               stateInfo.IsTag("Attack") ||
                               CheckStateNameContains(stateInfo, "attack");
        
        if (isInAttackState)
        {
            if (!_isAttacking)
            {
                _isAttacking = true;
                _hasCheckedThisAttack = false;
            }
            
            if (!_hasCheckedThisAttack && stateInfo.normalizedTime >= 0.85f && stateInfo.normalizedTime < 0.95f)
            {
                _hasCheckedThisAttack = true;
                CheckAttackHit();
            }
        }
        else
        {
            if (_isAttacking)
            {
                _isAttacking = false;
                _hasCheckedThisAttack = false;
            }
        }
    }
    
    private bool CheckStateNameContains(AnimatorStateInfo stateInfo, string keyword)
    {
        string stateName = GetCurrentStateName(stateInfo);
        return stateName.ToLower().Contains(keyword.ToLower());
    }
    
    private string GetCurrentStateName(AnimatorStateInfo stateInfo)
    {
        if (enemyAnimator == null) return "Unknown";
        
        int layerIndex = 0;
        AnimatorClipInfo[] clipInfos = enemyAnimator.GetCurrentAnimatorClipInfo(layerIndex);
        
        if (clipInfos.Length > 0)
        {
            return clipInfos[0].clip.name;
        }
        
        return $"State_{stateInfo.shortNameHash}";
    }
    
    private void CheckAttackHit()
    {
        if (IsPlayerInAttackRange())
        {
            KillPlayer();
        }
    }
    
    public void OnAttackAnimationEnd()
    {
        if (_hasKilledPlayer) return;
        if (!StarterAssets.StarterAssetsInputs.IsGameActive()) return;
        
        if (IsPlayerInAttackRange())
        {
            KillPlayer();
        }
    }
    
    private bool IsPlayerInAttackRange()
    {
        if (player == null) return false;
        
        float distanceToPlayer = Vector3.Distance(attackPoint.position, player.position);
        return distanceToPlayer <= attackRange;
    }
    
    private void KillPlayer()
    {
        _hasKilledPlayer = true;
        GameController.PauseGame(youreDeadPanel);
        
        if (deathText != null)
        {
            deathText.text = "YOU'RE DEAD!";
        }
    }
    
    private void OnRestartButtonClicked()
    {
        Debug.Log("🔄 RESTART CLICKED!");
        
        GameController.ClearAllPanels();
        
        if (youreDeadPanel != null)
        {
            youreDeadPanel.SetActive(false);
        }
        
        TeleportToCheckpoints();
        
        if (enemyAnimator != null && _hasIsAttackParameter)
        {
            enemyAnimator.SetBool(_isAttackHash, false);
        }
        
        _hasKilledPlayer = false;
        _isAttacking = false;
        _hasCheckedThisAttack = false;
        
        StarterAssets.StarterAssetsInputs.SetGameActive(true);
        
        Debug.Log("✅ RESTART COMPLETE!");
    }
    
    private void TeleportToCheckpoints()
    {
        if (player != null && playerCheckpoint != null)
        {
            CharacterController playerController = player.GetComponent<CharacterController>();
            if (playerController != null)
            {
                playerController.enabled = false;
                player.position = playerCheckpoint.position;
                player.rotation = playerCheckpoint.rotation;
                playerController.enabled = true;
            }
            else
            {
                player.position = playerCheckpoint.position;
                player.rotation = playerCheckpoint.rotation;
            }
        }
        
        if (enemyCheckpoint != null)
        {
            transform.position = enemyCheckpoint.position;
            transform.rotation = enemyCheckpoint.rotation;
        }
    }
    
    private void OnDrawGizmosSelected()
    {
        Transform point = attackPoint != null ? attackPoint : transform;
        
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(point.position, attackRange);
        
        if (player != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(point.position, player.position);
        }
    }
    }
}

