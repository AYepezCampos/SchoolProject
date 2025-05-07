using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class playerController : MonoBehaviour, IDamage
{
    [Header("----Components----")]
    [SerializeField] CharacterController controller;
    [SerializeField] AudioSource aud;
    [SerializeField] LayerMask ignoreMask;

    [Header("----Stats----")]
    [Range(1, 10)] [SerializeField] int HP;
    [Range(3, 5)][SerializeField] int speed;
    [Range(2, 5)][SerializeField] int sprintMod;
    [Range(1, 3)][SerializeField] int jumpMax;
    [Range(5, 20)][SerializeField] int jumpSpeed;
    [Range(15, 40)][SerializeField] int gravity;

    [Header("----Guns----")]
    [SerializeField] List<gunStats> gunList = new List<gunStats>();
    [SerializeField] GameObject gunModel;
    [SerializeField] GameObject muzzleFlash;
    [SerializeField] int shootDamage;
    [SerializeField] float shootRate;
    [SerializeField] int shootDist;

    [Header("----Audio----")]
    [SerializeField] AudioClip[] audSteps;
    [Range(0, 1)][SerializeField] float audStepsVol;
    [SerializeField] AudioClip[] audJump;
    [Range(0, 1)][SerializeField] float audJumpVol;
    [SerializeField] AudioClip[] audHurt;
    [Range(0, 1)][SerializeField] float audHurtVol;

    Vector3 moveDir;
    Vector3 playerVel;

    int jumpCount;
    int HPOrig;
    int selectGunPos;

    public int xp;

    bool isSprinting;
    bool isShooting;
    bool isPlayingStep;

    // Start is called before the first frame update
    void Start()
    {

        HPOrig = HP;
        updatePlayerUI();
        spawnPlayer();
    }

    public void spawnPlayer()
    {
        controller.enabled = false;
        transform.position = gameManager.instance.playerSpawnPos.transform.position;
        controller.enabled = true;
        HP = HPOrig;
        updatePlayerUI();

    }

    // Update is called once per frame
    void Update()
    {
        Debug.DrawRay(Camera.main.transform.position, Camera.main.transform.forward *shootDist, Color.yellow);

        if(!gameManager.instance.isPaused)
        {
            movement();
            selectGun();

        }

        sprint();

    }

    void movement()
    {
        //moveDir = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));
        //transform.position += moveDir * speed * Time.deltaTime;

        if (controller.isGrounded)
        {
            playerVel = Vector3.zero;
            jumpCount = 0;
        }

        moveDir = Input.GetAxis("Horizontal") * transform.right +
                    Input.GetAxis("Vertical") * transform.forward;
        controller.Move(moveDir * speed * Time.deltaTime);

        if (Input.GetButtonDown("Jump") && jumpCount < jumpMax)
        {
            jumpCount++;
            playerVel.y = jumpSpeed;
            aud.PlayOneShot(audJump[Random.Range(0, audJump.Length)], audJumpVol);
        }

        controller.Move(playerVel * Time.deltaTime);
        playerVel.y -= gravity * Time.deltaTime;

        if(Input.GetButton("Fire1") && gunList.Count > 0 && gunList[selectGunPos].ammoCur > 0 && !isShooting)
        {
           StartCoroutine(shoot());
        }

        if (controller.isGrounded && moveDir.magnitude > 0.3f && !isPlayingStep)
            StartCoroutine(playSteps());

    }

    IEnumerator playSteps()
    {
        isPlayingStep = true;

        //play the step sound
        aud.PlayOneShot(audSteps[Random.Range(0, audSteps.Length)], audStepsVol);

        if (!isSprinting)
        {
            yield return new WaitForSeconds(0.5f);

        }
        else
        {
            yield return new WaitForSeconds(0.3f);
        }
        isPlayingStep = false;
    }

    void sprint()
    {
        if(Input.GetButtonDown("Sprint"))
        {
            speed *= sprintMod;
            isSprinting = true;
        }
        else if (Input.GetButtonUp("Sprint"))
        {
            speed /= sprintMod;
            isSprinting = false;
        }
    }

    IEnumerator shoot()
    {
        isShooting = true;
        gunList[selectGunPos].ammoCur--;
        updatePlayerUI();
        StartCoroutine(flashMuzzle());

        aud.PlayOneShot(gunList[selectGunPos].shootSound[Random.Range(0, gunList[selectGunPos].shootSound.Length)], gunList[selectGunPos].shootVol);

        RaycastHit hit;
        if(Physics.Raycast(Camera.main.transform.position, Camera.main.transform.forward, out hit, shootDist, ~ignoreMask))
        {
            IDamage dmg = hit.collider.GetComponent<IDamage>();

            if(gunList[selectGunPos].hitEffect != null)
                Instantiate(gunList[selectGunPos].hitEffect, hit.point, Quaternion.identity);

            if(dmg != null)
            {
                dmg.takeDamage(shootDamage);
            }
        }

        yield return new WaitForSeconds(shootRate);
        isShooting = false;
    }

    public void takeDamage(int amount)
    {
        HP -= amount;
        aud.PlayOneShot(audHurt[Random.Range(0, audHurt.Length)], audHurtVol);
        updatePlayerUI();
        StartCoroutine(damageFlash());

        if(HP <= 0 )
        {
            // the player is dead
            gameManager.instance.youlose();
        }
    }

    IEnumerator damageFlash()
    {
        gameManager.instance.damagePanel.SetActive(true);
        yield return new WaitForSeconds(0.05f);
        gameManager.instance.damagePanel.SetActive(false);
    }

    IEnumerator flashMuzzle()
    {
        muzzleFlash.SetActive(true);
        yield return new WaitForSeconds(0.1f);
        muzzleFlash.SetActive(false);
    }

    public void updatePlayerUI()
    {
        gameManager.instance.playerHPBar.fillAmount = (float)HP / HPOrig;

        if(gunList.Count > 0 )
        {
            gameManager.instance.ammoCur.text = gunList[selectGunPos].ammoCur.ToString("F0");
            gameManager.instance.ammoMax.text = gunList[selectGunPos].ammoMax.ToString("F0");

        }
        
    }

    public void getGunStats(gunStats gun)
    {
        gunList.Add(gun);
        selectGunPos = gunList.Count - 1;
        updatePlayerUI();

        shootDamage = gun.shootDamage;
        shootDist = gun.shootDist;
        shootRate = gun.shootRate;

        gunModel.GetComponent<MeshFilter>().sharedMesh = gun.gunModel.GetComponent<MeshFilter>().sharedMesh;
        gunModel.GetComponent<MeshRenderer>().sharedMaterial = gun.gunModel.GetComponent <MeshRenderer>().sharedMaterial;
    }

    void selectGun()
    {
        if(Input.GetAxis("Mouse ScrollWheel") > 0 && selectGunPos < gunList.Count - 1)
        {
            selectGunPos++;
            changeGun();
        }
        else if (Input.GetAxis("Mouse ScrollWheel") < 0 && selectGunPos > 0)
        {
            selectGunPos--;
            changeGun();
        }
    }

    void changeGun()
    {
        updatePlayerUI();
        shootDamage = gunList[selectGunPos].shootDamage;
        shootDist = gunList[selectGunPos].shootDist;
        shootRate = gunList[selectGunPos].shootRate;

        gunModel.GetComponent<MeshFilter>().sharedMesh = gunList[selectGunPos].gunModel.GetComponent<MeshFilter>().sharedMesh;
        gunModel.GetComponent<MeshRenderer>().sharedMaterial = gunList[selectGunPos].gunModel.GetComponent<MeshRenderer>().sharedMaterial;
    }

}