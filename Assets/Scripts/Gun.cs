using UnityEngine;

public class Gun : MonoBehaviour
{

    public float damage = 10f;
    public float range = 100f;

    int ammo = 1000;

    public Camera fpsCam;
    public ParticleSystem muzzleFlash;

	public Transform barrel;
	public LineRenderer line;
	private float lineDuration = 0.1f;
	private float lineTimer = 0f;

    

    // Update is called once per frame
    void Update()
    {

        if (Input.GetButtonDown("Fire1"))
        {
            Shoot();
        }
		if (lineTimer > 0)
		{
			lineTimer -= Time.deltaTime;
			if (lineTimer <= 0)
			{
				line.SetPosition(0, Vector3.zero);
				line.SetPosition(1, Vector3.zero);
			}
		}
    }

    void Shoot()
    {
        if (ammo <= 0) { return; }
        ammo -= 1;
        

        muzzleFlash.Play();

        RaycastHit hit;
        if (Physics.Raycast(fpsCam.transform.position, fpsCam.transform.forward, out hit, range))
        {
            Debug.Log(hit.transform.name);

            Target target = hit.transform.GetComponent<Target>();
            if (target != null)
            {
                target.TakeDamage(damage);
            } else
            {
				SplatMesher.instance.AddSplat(hit);
            }

			line.SetPosition(0, barrel.position);
			line.SetPosition(1, hit.point);
			lineTimer = lineDuration;
        }

    }
}
