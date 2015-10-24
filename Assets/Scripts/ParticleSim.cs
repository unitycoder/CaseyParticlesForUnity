using UnityEngine;
using System.Collections;

/* 
 * HandMadeHero Particle test with Unity
 * Tutorial from HandMadeHero broadcast: https://www.youtube.com/watch?v=G6OGKP3MaUI
 * Unity3D version by UnityCoder.com
 * */

public class ParticleSim : MonoBehaviour {

	bool debugMode = true;

	public bool enableDispersion = true;
	public Color initialColor = new Vector4(1,1,0,2);

	bool groundPlaneCollision = true;

	const int particlesAmount = 256;
	int spawnRate = 3;

//	float maxSpeed = 1.5f;
//	public Transform[] obstacles;

    const int particle_Cel_Dim = 32;
	int NextParticle=0;
	particle[] particles = new particle[particlesAmount];
    particle_cel[,] ParticleCels = new particle_cel[particle_Cel_Dim, particle_Cel_Dim];

	public Transform sprite;
	SpriteRenderer[] sprites;

	const float gridScale = 0.25f;
	float InvGridScale = 1f/gridScale;

	float groundY = 0;


	void Start () 
	{
	
		sprites = new SpriteRenderer[particlesAmount];

		// spawn particles away from the screen
		for (int i = 0; i < sprites.Length; i++) 
		{
			var clone = Instantiate(sprite,Vector3.one*999,Quaternion.identity) as Transform;
			// take references to the spawned particle
			sprites[i] = clone.GetComponent<SpriteRenderer>();
		}
	}
	

	void Update () 
	{

		Vector3 gridOrigin = new Vector3(-0.5f *gridScale * particle_Cel_Dim, 0f, 0f);

        // reset array
       // ParticleCels = new particle_cel[Particle_Cel_Dim, Particle_Cel_Dim];      
		for (int y = 0; y < particle_Cel_Dim; ++y)
        {
			for (int x = 0; x < particle_Cel_Dim; ++x)
            {
				ParticleCels[y, x].Density = 0;
				//ParticleCels[y, x].VelocityTimesDensity = Vector3.zero;


				/*
				// borders test
				if (x==0 || y==0 || x==Particle_Cel_Dim-1 || y==Particle_Cel_Dim-1)
				{
	                ParticleCels[y, x].Density = 1;
				}else{
					ParticleCels[y, x].Density = 0;
				}*/

				/*
				// update obstacle maps test
				for (int i = 0; i < obstacles.Length; i++) 
				{
					float distance = (new Vector3(x,y,0)-(obstacles[i].position-GridOrigin)*InvGridScale).sqrMagnitude*2f;
					if (distance>Particle_Cel_Dim) distance=Particle_Cel_Dim;
					ParticleCels[y, x].Density += Mathf.Clamp((Particle_Cel_Dim-distance),0,Particle_Cel_Dim);
				}
 
				// test perlin map 
				ParticleCels[y, x].Density = Mathf.PerlinNoise(2.2f+x*0.06f,2.2f+y*0.06f);
				if (ParticleCels[y, x].Density<0.4f) ParticleCels[y, x].Density=0;
				if (ParticleCels[y, x].Density>0.4f) ParticleCels[y, x].Density*=9999f;
				*/

            }
        }

        // (re)spawn particle
		for (int particleSpawnIndex = 0; particleSpawnIndex < spawnRate; ++particleSpawnIndex)
		{
			NextParticle=(NextParticle+1) % particlesAmount;

			// set initial values
			particles[NextParticle].P = transform.position+new Vector3(Random.Range(-0.05f, 0.05f), 0, 0); // start position
//			particles[NextParticle].dP = new Vector3(Random.Range(-0.07f,0.07f),0.85f,0)*7; // start speed, global direction
			particles[NextParticle].dP = transform.TransformDirection(new Vector3(Random.Range(-0.01f,0.01f),7f*Random.Range(0.7f,1f),0f)); // start speed in local transform direction
			particles[NextParticle].ddP = new Vector3(0f, -9.81f, 0f); // gravity
			particles[NextParticle].Col = initialColor;
			float randomVal = Random.Range(-0.25f, -1f);
			particles[NextParticle].dColor = new Vector4(0,0,0,randomVal); // deltaColor

            sprites[NextParticle].transform.position = Vector3.zero;
			sprites[NextParticle].transform.localScale = Vector3.one;
		}


        // map particles to grid & add cell density
        for (int particleIndex = 0; particleIndex < particles.Length; ++particleIndex)
        {
            Vector3 P = InvGridScale * (particles[particleIndex].P - gridOrigin);

            int X = (int)P.x;
            int Y = (int)P.y;

            if (X < 0) X = 0;
            if (X > particle_Cel_Dim-1) X = particle_Cel_Dim-1;
            if (Y < 0) Y = 0;
            if (Y > particle_Cel_Dim - 1) Y = particle_Cel_Dim - 1;

			float density = particles[particleIndex].Col.w;

            ParticleCels[Y, X].Density += density;
            ParticleCels[Y, X].VelocityTimesDensity += density*particles[particleIndex].dP;
        }


		if (debugMode)
		{
			// draw preview grid
			for (int y = 0; y < particle_Cel_Dim; ++y)
	        {
	            for (int x = 0; x < particle_Cel_Dim; ++x)
	            {

	                float alpha = Clamp01(0.1f*ParticleCels[y, x].Density);
	                Color cellColor = new Color(alpha,alpha,alpha,1);

	                Vector3 gridPos = gridScale*new Vector3(x, y, 0f) + gridOrigin;

					Debug.DrawLine(gridPos,gridPos+new Vector3(1,0,0)*gridScale,cellColor);
					Debug.DrawLine(gridPos, gridPos + new Vector3(0, 1, 0)*gridScale, cellColor);
	            }
	        }
		}


        // simulate
		for (int particleIndex = 0; particleIndex < particles.Length; ++particleIndex)
		{
			// snap to grid
			Vector3 P = InvGridScale * (particles[particleIndex].P - gridOrigin);
			int X = (int)P.x;
			int Y = (int)P.y;

			// clamp to edges
			if (X < 1) X = 1;
			if (X > particle_Cel_Dim-2) X = particle_Cel_Dim-2;
			if (Y < 1) Y = 1;
			if (Y > particle_Cel_Dim - 2) Y = particle_Cel_Dim - 2;

			// get neighbour cell values
			float celCenterDensity = ParticleCels[Y, X].Density;
			float celLeftDensity = ParticleCels[Y, X-1].Density;
			float celRightDensity = ParticleCels[Y, X+1].Density;
			float celDownDensity = ParticleCels[Y-1, X].Density;
			float celUpDensity = ParticleCels[Y+1, X].Density;

			// calculate direction from cell density
			Vector3 dispersion = Vector3.zero;
			float dispersionCoeffifient = 1f; // multiplier of pushing force

			dispersion += dispersionCoeffifient*(celCenterDensity-celLeftDensity)*new Vector3(-1f,0f,0f);
			dispersion += dispersionCoeffifient*(celCenterDensity-celRightDensity)*new Vector3(1f,0f,0f);
			dispersion += dispersionCoeffifient*(celCenterDensity-celDownDensity)*new Vector3(0f,-1f,0f);
			dispersion += dispersionCoeffifient*(celCenterDensity-celUpDensity)*new Vector3(0f,1f,0f);

			Vector3 ddP = particles[particleIndex].ddP+(enableDispersion?dispersion:Vector3.zero);

			// debug test
//			if (particleIndex<1) Debug.Log(particles[particleIndex].dP);

			// calculate movement & color
			particles[particleIndex].P += (0.5f*Mathf.Sqrt(Time.deltaTime)*Time.deltaTime*ddP + Time.deltaTime*particles[particleIndex].dP);
			particles[particleIndex].dP += Time.deltaTime*ddP;
			particles[particleIndex].Col += Time.deltaTime*particles[particleIndex].dColor;

            // ground collision & bounce
			if (groundPlaneCollision && particles[particleIndex].P.y<groundY)
            {
                float coefficientOfRestitution = 0.3f;
				float coefficientOfFriction = 0.7f;
				particles[particleIndex].P.y = -particles[particleIndex].P.y;
				particles[particleIndex].dP.y = -coefficientOfRestitution*particles[particleIndex].dP.y;
				particles[particleIndex].dP.x = coefficientOfFriction*particles[particleIndex].dP.x;
			}

			/*
			// TEST max speed clamp
			if (particles[ParticleIndex].dP.sqrMagnitude>maxSpeed)
			{
				particles[ParticleIndex].dP = particles[ParticleIndex].dP.normalized*maxSpeed;
			}*/

			// clamp colors
			particles[particleIndex].Col.x = Clamp01(particles[particleIndex].Col.x);
			particles[particleIndex].Col.y = Clamp01(particles[particleIndex].Col.y);
			particles[particleIndex].Col.z = Clamp01(particles[particleIndex].Col.z);
			particles[particleIndex].Col.w = Clamp01(particles[particleIndex].Col.w);

            // fix particle sudden spawn by making it transparent first, not working smoothly
            if (particles[particleIndex].Col.w>0.9f)
            {
                particles[particleIndex].Col.w = 0.9f*Clamp01MapToRange(1f,(particles[particleIndex].Col.w),0.9f);
            }

			// set position, color, scale
			sprites[particleIndex].transform.position = particles[particleIndex].P;
			sprites[particleIndex].color = (Color)particles[particleIndex].Col;
            var scaler = particles[particleIndex].Col.w;
			sprites[particleIndex].transform.localScale = new Vector3(scaler, scaler, scaler)*0.75f; // scale test
		}
	}



    // helper functions
    float Clamp01MapToRange(float Min, float t, float Max)
    {

        // temporary fix for negative values
        float tempMin = Min;
        Min = Mathf.Min(Min, Max);
        Max = Mathf.Max(tempMin, Max);


        float Result = 0f;
        float Range = Max - Min;

        //Debug.Log(Range);

        if (Range != 0f)
        {
            Result = Clamp01((t - Min) / Range);
        }

        return Result;
    }

    float Clamp01(float Value)
    {
        float Result = Clamp(0f, Value, 1f);

        return Result;
    }

    float Clamp(float Min, float Value, float Max)
    {
        float Result = Value;

        if (Result<Min)
        {
            Result = Min;
        }else if (Result>Max)
        {
            Result = Max;
        }

        return Result;
    }

}
