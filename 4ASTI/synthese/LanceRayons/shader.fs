
precision mediump float;


uniform vec3 uOriVector;
uniform int uTime;

varying vec3 pixCenter;

/*
	#################################
	#################################
				STRUCTURES
	#################################
	#################################
*/

// ==================================
struct ray
{
	vec3 O; // origine du rayon
	vec3 V; // point de destination
	float t;
};

// ==================================
struct plan
{
	vec3 S; // quelconque appartenant au plan
	vec3 n; // vecteur normal au plan
	vec3 color;
};

// ==================================
struct sphere
{
	vec3  C;     // Centre du cercle
	vec3  color;
	float r;     // Rayon
};

// ==================================
struct source
{
	vec3 pos; // Position de la source
	vec3 pow; // Puissance de la source

};


/*
	#################################
	#################################
				GLOBAL
	#################################
	#################################
*/

int NB_SOURCES = 0;
int NB_SPHERES = 0;
int NB_PLANS   = 0;

float VITESSE = float(uTime)/50.0;
float PI      = 3.14;

// ==================================
sphere T_SPH[100];
source T_SRC[100];
plan   T_PLANS[100];


/*
	#################################
	#################################
			FONCTIONS TIERS
	#################################
	#################################
*/
// ==================================
float rayPlan(ray r, plan p)
{
	vec3 OS     = p.S - r.O; // Vecteur OS
	float denom = dot(p.n, r.V);
	if(denom != 0.0)
	{
		float t = (dot(OS, p.n) / denom);
		return t;
	}
	return -1.0;
}

// ==================================
float raySphere(ray r, sphere s)
{
	vec3 OC = r.O - s.C;
	float A = dot(r.V, r.V);         // norme de V au carre
	float B = 2.0 * dot(OC, r.V);    // 2*(OC.V)
	float C = dot(OC, OC) - (s.r*s.r); // norme(OC)*norme(OC) - r*r

	float delta = (B*B) - (4.0*A*C);

	if(delta >= 0.0)
	{
		float t1 = (-B - sqrt(delta)) / (2.0 * A);
		float t2 = (-B + sqrt(delta)) / (2.0 * A);

		if(t1 < 0.0 && t2 < 0.0) return -1.0;
		else if(t1 < 0.0)        return t2;
		else if(t2 < 0.0)        return t1;
		else                     return min(t1,t2);
	}
	return -1.0;
}

// ==================================
void eclaire_ombre_point(ray r_src, vec3 pt_repere, vec3 kd)
{
	vec3 LO       = vec3(0.0, 0.0, 0.0);
	vec3 I        = r_src.O + r_src.t*r_src.V;
	bool shadowed = false;

	// Pour chaque rayon de source de lumiere...
	for(int i=0; i<1000; i++)
	{
		if(i>=NB_SOURCES) break;

		ray r_lum = ray(I, T_SRC[i].pos, -1.0);
		shadowed = false;

		// ... on verifie si une sphere le bloque ...
		for(int j=0; j<1000; j++)
		{
			if(j>=NB_SPHERES) break;

			r_lum.t  = raySphere(r_lum, T_SPH[j]);
			if( (r_lum.t >= 0.0) && (pt_repere != T_SPH[j].C) )
			{
				shadowed = true;
				break;
			}
		}

		if(shadowed) break;
		// ... ou si un plan le bloque ...
		for(int j=0; j<1000; j++)
		{
			if(j>=NB_PLANS) break;

			r_lum.t = rayPlan(r_lum, T_PLANS[j]);
			if( (r_lum.t >= 0.0) && (pt_repere != T_PLANS[j].S) )
			{
				shadowed = true;
				break;
			}
		}

		if(shadowed) break;
		// .. sinon on calcule la luminance resultante

		// Eclairage
		vec3   N        = normalize(I - pt_repere);
		vec3   V        = normalize(T_SRC[i].pos - I);
		float cosThetaI = dot(N,V);
		
		// Brillance
		vec3 V0         = normalize(-r_src.V);
		vec3 H          = normalize(V0+V);
		float cosAlphaI = dot(H,N);

		// Calcul de la reflectance
		vec3 ks = vec3(0.1, 0.1, 0.1);
		float n = 5.0;
		vec3 reflectance = (kd/PI) + ((n+2.0)/(2.0*PI))*ks*pow(cosAlphaI, n);

		// Addition d'une source de lumiere
		LO += T_SRC[i].pow * reflectance * cosThetaI; 
	}
	
	// L0 est au final la somme de toutes les luminances recue
	gl_FragColor = vec4(LO, 1.0);
}

// ==================================
void draw_raw_all(ray rayon)
{
	bool   min_init_s = false;
	bool   min_init_p = false;
	float  min_t_s    = 0.0;
	float  min_t_p    = 0.0;
	float  tmp_t      = 0.0;
	sphere min_sph;
	plan   min_plan;


	// Recherche du t min et de la sphere s associee
 	for(int i=0; i<100; i++)
	{
		if(i>=NB_SPHERES) break;

		if(!min_init_s) // Recherche premiere valeur positive
		{
			min_t_s = raySphere(rayon, T_SPH[i]);
			if(min_t_s >= 0.0)
			{
				min_sph  = T_SPH[i];
				min_init_s = true;
			}
		}
		else // Recherche du min definitif
		{
			tmp_t = raySphere(rayon, T_SPH[i]);
			if((tmp_t >= 0.0) && (tmp_t < min_t_s))
			{
				min_t_s   = tmp_t;
				min_sph = T_SPH[i];
			}
		}		
	}

	// Recherche du t min et du plan associe
 	for(int i=0; i<100; i++)
	{
		if(i>=NB_PLANS) break;

		if(!min_init_p) // Recherche premiere valeur positive
		{
			min_t_p = rayPlan(rayon, T_PLANS[i]);
			if(min_t_p >= 0.0)
			{
				min_plan   = T_PLANS[i];
				min_init_p = true;
			}
		}
		else // Recherche du min definitif
		{
			tmp_t = rayPlan(rayon, T_PLANS[i]);
			if((tmp_t >= 0.0) && (tmp_t < min_t_p))
			{
				min_t_p  = tmp_t;
				min_plan = T_PLANS[i];
			}
		}		
	}

	if(min_init_p && min_init_s) // min trouve chez les deux types
	{
		if(min_t_s < min_t_p) gl_FragColor = vec4(min_sph.color,  1.0);
		else                  gl_FragColor = vec4(min_plan.color, 1.0);
	}
	else if(min_init_s)       gl_FragColor = vec4(min_sph.color,  1.0);
	else if(min_init_p)       gl_FragColor = vec4(min_plan.color, 1.0);
	else                      gl_FragColor = vec4(0.0, 0.0, 0.0,  1.0);
}

// ==================================
void draw_shined_all(ray rayon)
{
	bool   min_init_s = false;
	bool   min_init_p = false;
	float  min_t_s    = 0.0;
	float  min_t_p    = 0.0;
	float  tmp_t      = 0.0;
	sphere min_sph;
	plan   min_plan;


	// Recherche du t min et de la sphere s associee
 	for(int i=0; i<100; i++)
	{
		if(i>=NB_SPHERES) break;

		if(!min_init_s) // Recherche premiere valeur positive
		{
			min_t_s = raySphere(rayon, T_SPH[i]);
			if(min_t_s >= 0.0)
			{
				min_sph  = T_SPH[i];
				min_init_s = true;
			}
		}
		else // Recherche du min definitif
		{
			tmp_t = raySphere(rayon, T_SPH[i]);
			if((tmp_t >= 0.0) && (tmp_t < min_t_s))
			{
				min_t_s   = tmp_t;
				min_sph = T_SPH[i];
			}
		}		
	}

	// Recherche du t min et du plan associe
 	for(int i=0; i<100; i++)
	{
		if(i>=NB_PLANS) break;

		if(!min_init_p) // Recherche premiere valeur positive
		{
			min_t_p = rayPlan(rayon, T_PLANS[i]);
			if(min_t_p >= 0.0)
			{
				min_plan   = T_PLANS[i];
				min_init_p = true;
			}
		}
		else // Recherche du min definitif
		{
			tmp_t = rayPlan(rayon, T_PLANS[i]);
			if((tmp_t >= 0.0) && (tmp_t < min_t_p))
			{
				min_t_p  = tmp_t;
				min_plan = T_PLANS[i];
			}
		}		
	}

	if(min_init_p && min_init_s) // min trouve chez les deux types
	{
		if(min_t_s < min_t_p) 
		{
			rayon.t = min_t_s;
			eclaire_ombre_point(rayon, min_sph.C,  min_sph.color);
		}
		else
		{
			rayon.t = min_t_p;
			eclaire_ombre_point(rayon, min_plan.S, min_plan.color);
		}
	}
	else if(min_init_s)
	{
		rayon.t = min_t_s;
		eclaire_ombre_point(rayon, min_sph.C,  min_sph.color);
	}
	else if(min_init_p)
	{
		rayon.t = min_t_p;
		eclaire_ombre_point(rayon, min_plan.S, min_plan.color);
	}       
	else gl_FragColor = vec4(0.0, 0.0, 0.0,  1.0);
}

/*
	##################################
	##################################
				SCENES
	##################################
	##################################
*/
// ===================================
void scene_Test(void)
{
	// ===== Par defaut la scene est blanche
	gl_FragColor  = vec4(1.0, 1.0, 1.0, 1.0) ;

	// Init des structs
	ray    rayon  = ray(uOriVector, pixCenter, -1.0);

	// plan1 est legerement incline vers la droite et est en dessous de la vue
	plan   plan1  = plan(vec3(0.0, 0.0, -10.0), vec3(0.10, 0.0, 0.75), vec3(0.6, 0.3, 0.2));
	//plan   plan2  = plan(vec3(0.0, 0.0, 40.0), vec3(0.0, -1.0, 0.0), vec3(0.1, 0.1, 0.9));

	sphere sph0   = sphere(vec3(-20.0, 240.0+float(uTime), 0.0), vec3(0.8, 0.1, 0.1), 50.0);
	sphere sph1   = sphere(vec3(0.0, 200.0, 10.0), vec3(0.1, 0.1, 0.8), 10.0);
	T_SPH[0]      = sph0;
	T_SPH[1]      = sph1;

	// Sources de lumieres
	//T_SRC[0] = source(vec3(40.0, 50.0, 0.0), vec3(5.0, 5.0, 5.0));
	T_SRC[0] = source(vec3(30.0, 80.0, 10.0), vec3(10.0, 10.0, 10.0));
	//T_SRC[0] = source(vec3(0.0, 180.0, 10.0),   vec3(5.0, 5.0, 5.0));

	// Spheres
	T_SPH[0]   = sphere(vec3(0.0, 80.0, 10.0), vec3(0.8, 0.1, 0.1), 5.0);
	T_SPH[1]   = sphere(vec3(10.0, 80.0, 10.0), vec3(0.1, 0.1, 0.8), 1.0);
	//T_SPH[2]   = sphere(lumiere, vec3(0.1, 0.1, 0.8), 1.0);

	// ===== Dessin
	draw_raw_all(rayon);
	//draw_shined_all(rayon);
}

// ==================================

void scene_SateliteOrbital(void) // Manque rotation orbitale  float(uTime) modulo
{
	// ===== Par defaut la scene est noire
	gl_FragColor  = vec4(0.1, 0.1, 0.1, 1.0) ;

	// ===== Init des structs
	ray    rayon  = ray(uOriVector, pixCenter, -1.0);

	// Plans
	NB_PLANS      = 0;

	// Spheres
	//Planete Bleue
	sphere sph0   = sphere(vec3(-10.0, 1000.0, 0.0), vec3(0.0, 0.0, 0.8), 25.0);

	// Sattelite Orange
	sphere sph1   = sphere(vec3(sph0.C.x + (sph0.r + 250.0)*cos(VITESSE), sph0.C.y + (sph0.r + 250.0)*sin(VITESSE), sph0.C.z), vec3(0.88, 0.41, 0.0), 35.0);

	// Sattelite Vert
	sphere sph2   = sphere(vec3(sph0.C.x + (sph0.r + 100.0)*cos(VITESSE*1.4), sph0.C.y + (sph0.r + 100.0)*sin(VITESSE*1.4), sph0.C.z), vec3(0.62, 1.85, 0.7), 20.0);

	// Sattelite Blanc de Orange
	sphere sph3   = sphere(vec3(sph1.C.x + (sph1.r + 20.0)*cos(VITESSE*3.0), sph1.C.y + (sph1.r + 20.0)*sin(VITESSE*3.0), sph1.C.z), vec3(1.0, 1.0, 1.0), 5.0);

	T_SPH[0]      = sph0;
	T_SPH[1]      = sph1;
	T_SPH[2]      = sph2;
	T_SPH[3]      = sph3;

	NB_SPHERES    = 4;

	// ===== Init des sources de lumiere
	T_SRC[0] = source(vec3(200.0, 100.0, 0.0), vec3(7.0, 7.0, 7.0));

	NB_SOURCES = 1;

	// ===== Dessin
	//draw_raw_all(rayon);
	draw_shined_all(rayon);
}

void scene_Boite_Collier(void) // Rotation du collier horaire et changement des couleurs
{
	// ===== Par defaut la scene est blancche
	gl_FragColor  = vec4(1.0, 1.0, 1.0, 1.0) ;

	// Init des structs
	ray    rayon  = ray(uOriVector, pixCenter, -1.0);

	// 5 plans pour la boite

	// 5 spheres pour le collier

	sphere sph0   = sphere(vec3(50.0, 250.0, 10.0), vec3(0.88, 0.41, 0.0), 10.0);
	sphere sph1   = sphere(vec3(50.0, 250.0, 10.0), vec3(0.88, 0.41, 0.0), 10.0);
	sphere sph2   = sphere(vec3(50.0, 250.0, 10.0), vec3(0.88, 0.41, 0.0), 10.0);
	sphere sph3   = sphere(vec3(50.0, 250.0, 10.0), vec3(0.88, 0.41, 0.0), 10.0);
	sphere sph4   = sphere(vec3(50.0, 250.0, 10.0), vec3(0.88, 0.41, 0.0), 10.0);
	T_SPH[0]      = sph0;
	T_SPH[1]      = sph1;
	T_SPH[2]      = sph2;
	T_SPH[3]      = sph3;
	T_SPH[4]      = sph4;

	NB_SPHERES = 5;

	// ===== Init des sources de lumiere
	T_SRC[0] = source(vec3(200.0, 100.0, 0.0), vec3(10.0, 10.0, 10.0));

	NB_SOURCES = 1;

	// ===== Dessin
	//draw_raw_all(rayon);
	draw_shined_all(rayon);
}

void scene_Lueures(void)
{
	// Init des structs
	ray rayon   = ray(uOriVector, pixCenter, -1.0);

	// 1 plan pour le sol
	T_PLANS[0]  = plan(vec3(0.0, 0.0, -30.0), vec3(0.0, 0.0, 1.0), vec3(0.2, 0.2, 0.2));
	NB_PLANS    = 1;

	// 1 sphere cenrale + 2 tournantes
	sphere sph0 = sphere(vec3(0.0, 200.0, 0.0), vec3(0.9, 0.9, 0.9), 30.0);
	sphere sph1 = sphere(vec3(sph0.C.x + (sph0.r + 50.0)*cos(VITESSE), sph0.C.y + (sph0.r + 50.0)*sin(VITESSE), sph0.C.z), vec3(10.0, 0.0, 0.0), 1.0);
	sphere sph2 = sphere(vec3(sph0.C.x - (sph0.r + 50.0)*cos(VITESSE), sph0.C.y + (sph0.r + 50.0)*sin(VITESSE), sph0.C.z), vec3(0.0, 0.0, 10.0), 1.0);
	T_SPH[0]    = sph0;
	T_SPH[1]    = sph1;
	T_SPH[2]    = sph2;
	NB_SPHERES  = 3;

	// ===== Init des sources de lumiere
	T_SRC[0]    = source(vec3(200.0, 100.0, 0.0), vec3(3.0, 3.0, 3.0));
	T_SRC[1]    = source(vec3(sph0.C.x + (sph0.r + 50.0)*cos(VITESSE), sph0.C.y + (sph0.r + 50.0)*sin(VITESSE), sph0.C.z), vec3(10.0, 0.0, 0.0));
	T_SRC[2]    = source(vec3(sph0.C.x - (sph0.r + 50.0)*cos(VITESSE), sph0.C.y + (sph0.r + 50.0)*sin(VITESSE), sph0.C.z), vec3(0.0, 0.0, 10.0));
	NB_SOURCES  = 3;


	// ===== Dessin
	//draw_raw_all(rayon);
	draw_shined_all(rayon);
}



// ==================================
void main(void)
{
	//scene_Test();
	scene_SateliteOrbital();
	//scene_Boite_Collier();
	//scene_Lueures();
	//scene_atome();
}
