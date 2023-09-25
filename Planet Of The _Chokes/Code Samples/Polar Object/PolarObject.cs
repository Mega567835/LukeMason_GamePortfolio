using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class PolarObject : MonoBehaviour
{
    //cartesian coords
    public float x; 
    public float y;

    //polar coords
    public float theta; //angle from x axis
    public float r; //how high up

    //angle object is facing
    public float dir; 
    public float dirdif; //relative to world

    //scale of object
    public float width;
    public float height;
    private float rScale = 5.0f; //Helps determine where stacking towers go

    //objects all have health - convert to composition later
    public float maxHealth;
    private float health;

    public Vector2 getRect() 
    {
        Vector2 toRet = new Vector2(x, y);
        return toRet;
    }

    //convert polar to rectangular
    public Vector2 toRect(float mag, float dir)
    {
        Vector2 toRet = new Vector2(0, 0);
        toRet[0] = mag * Mathf.Cos(dir);
        toRet[1] = mag * Mathf.Sin(dir);
        return toRet;
    }

    //set position given cartesian coordinates
    public bool setPosxy(float newX, float newY)
    {
        float oldtheta = theta;
        x = newX;
        y = newY;
        theta = Mathf.Atan2(x,  y);
        r = Mathf.Sqrt(Mathf.Pow(x, 2) + Mathf.Pow(y, 2)) / rScale;
        this.transform.Rotate(Vector3.forward, Mathf.Rad2Deg * (theta - oldtheta));
        this.transform.position = new Vector2(x, y);
        return true;
    }

    //set position given polar coordinates
    public bool setPosPol(float newR, float newTheta)
    {
        float oldtheta = theta;
        this.r = newR;
        theta = newTheta;
        angle = newTheta - theta;
        this.transform.Rotate(Vector3.forward, Mathf.Rad2Deg * angle);
        x = r * Mathf.Cos(theta * Mathf.PI / 180);
        y = r * Mathf.Sin(theta * Mathf.PI / 180);
        this.transform.position = new Vector2(x, y);
        return true;
    }

    //initialization runs before scene starts
    protected virtual void Start()
    {   
        this.transform.Rotate(Vector3.forward, -90); // since rotation starts at x axis we need to fix every object's angle at the start
        height = 1f;
        width = 1f;
        this.health = this.maxHealth;
    }

    public void takeDamage(float dmg)
    {
        this.health -= dmg;

        //recursive calls death on any towers stacked on top of this one (if it is a tower)
        if(this.health <= 0)
        {
            killChildren();
        }
    }
    //tower will inherit and change - we realized that we should have done composition for this stuff so this is not great code
    public virtual void killChildren()
    {
        Destroy(this.gameObject);
    }
}
