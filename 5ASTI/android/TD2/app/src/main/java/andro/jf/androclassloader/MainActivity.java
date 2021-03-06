package andro.jf.androclassloader;


import java.io.File;
import java.lang.ref.WeakReference;
import java.lang.reflect.Field;

import android.app.Activity;
import android.content.Context;
import android.content.Intent;
import android.os.Bundle;
import android.util.ArrayMap;
import android.util.Log;
import android.view.Menu;
import android.view.MenuItem;
import android.view.View;
import android.view.View.OnClickListener;
import android.widget.Button;
import android.widget.TextView;
import dalvik.system.DexClassLoader;

public class MainActivity extends Activity {

	// For the lab, the original external APK is:
	private static final String SECONDARY_DEX_NAME = "AndroidTest-ORIGIN2.apk";

	// Attack 1:
	// Difficulty: medium
	//private static final String SECONDARY_DEX_NAME = "AndroidTest-HACK1-EN.apk";

	// Attack 2:
	// Difficulty: difficult
	// private static final String SECONDARY_DEX_NAME = "AndroidTest-HACK2.apk";

	// Attaque 3:
	// Difficulty: easy
	// private static final String SECONDARY_DEX_NAME = "AndroidTest-HACK3.apk";

	// URL of the distant APK
	private static final String IP_ADRESSE = "https://gitlab.inria.fr/jlalande/teaching-android-mobile-security/raw/master/CLASS/" + SECONDARY_DEX_NAME;

    // ==============================================================================================
	private Profil myProfile = new Profil("JFL", 36);

	public static Profil myProfileForReadOnlyPurpose = null;

	// The name of distant class that is used by the second activity
	private static final String CLASSE_NAME = "andro.jf.appli.RandomActivity";

	@Override
	protected void onCreate(Bundle savedInstanceState){
		super.onCreate(savedInstanceState);
		setContentView(R.layout.activity_main);
		Log.i("JFL", "I am in MainActivity !");

		// Loading my info
		myProfile.setCreditCard(new CreditCard(123456789)); // loading my credit card number
		TextView nV = (TextView)findViewById(R.id.number);
		nV.setText("" + myProfile.getCreditCard().getNumber());
		
		// Copying profile for READ ONLY purpose of 
		// the name of the Profil in Activity 2 (RandomActivity)
		// This will also protect the myProfile reference
		try {
			myProfileForReadOnlyPurpose = (Profil)myProfile.clone();
		} catch (CloneNotSupportedException e) {
			e.printStackTrace();
		}

		// Loading an inexistent local class
		// Asynchronous loading of the package from the sitant apk file
		final File dexInternalStoragePath = new File(getDir("dex", Context.MODE_PRIVATE),SECONDARY_DEX_NAME);
		LoadAsyncTask myTask = new LoadAsyncTask();
		myTask.execute(IP_ADRESSE,dexInternalStoragePath.toString(),"/"+SECONDARY_DEX_NAME, this);
		
		Button profil = (Button)findViewById(R.id.button2);
		profil.setOnClickListener(new OnClickListener() {
			
			@Override
			public void onClick(View v) {
				Intent i = new Intent(getApplicationContext(), MonProfil.class);
				startActivity(i);				
			}
		});
	}

	// Called method at the end of the downloading
	public static void loadExternalClass(final Context context){
		final File optimizedDexOutputPath =	new File(context.getFilesDir().getAbsolutePath());
		final File dexInternalStoragePath = new File(context.getDir("dex", Context.MODE_PRIVATE),SECONDARY_DEX_NAME);

		// Creating the DexClassloader after the downloading
		final ClassLoader customCL = new CustomClassLoader(
				dexInternalStoragePath,
				optimizedDexOutputPath,
				null,
				context.getClassLoader());

		// Set customCl permanent (for being used by other classes in other activities)
		setAPKClassLoader(customCL, context);

		try {

			// Loading distant classes
			final Class<?> classtest = customCL.loadClass(CLASSE_NAME);
			Log.i("JFL", classtest.getName() + "loaded !");

			// Implementing the button of activity 2
			Button b = (Button)((Activity) context).findViewById(R.id.button1);
			b.setEnabled(true);
			b.setOnClickListener(new OnClickListener() {

				@Override
				public void onClick(View v) {
					
					Intent intent = new Intent(context, classtest);
					context.startActivity(intent);
				}
			});
		} catch (ClassNotFoundException e) {
			e.printStackTrace();
		} catch (IllegalArgumentException e) {
			e.printStackTrace();
		}
	}

	private static void setAPKClassLoader(ClassLoader classLoader, Context context)
	{
		try {
			Field mMainThread = getField(Activity.class, "mMainThread");
			Object mainThread = mMainThread.get(context);
			Class<?> threadClass = mainThread.getClass();
			Field mPackages = getField(threadClass, "mPackages");

			ArrayMap<String,?> map = (ArrayMap<String,?>) mPackages.get(mainThread);
			WeakReference<?> ref = (WeakReference<?>) map.get(context.getPackageName());
			Object apk = ref.get();
			Class<?> apkClass = apk.getClass();
			Field mClassLoader = getField(apkClass, "mClassLoader");

			mClassLoader.set(apk, classLoader);
		} catch (IllegalArgumentException e) {
			e.printStackTrace();
		} catch (IllegalAccessException e) {
			e.printStackTrace();
		}
	} 

	private static Field getField(Class<?> cls, String name)
	{
		for (Field field: cls.getDeclaredFields())
		{
			if (!field.isAccessible()) {
				field.setAccessible(true);
			}
			if (field.getName().equals(name)) {
				return field;
			}
		}
		return null;
	}

	@Override
	public boolean onCreateOptionsMenu(Menu menu) {
		// Inflate the menu; this adds items to the action bar if it is present.
		getMenuInflater().inflate(R.menu.main, menu);
		return true;
	}

	@Override
	public boolean onOptionsItemSelected(MenuItem item) {
		// Handle action bar item clicks here. The action bar will
		// automatically handle clicks on the Home/Up button, so long
		// as you specify a parent activity in AndroidManifest.xml.
		int id = item.getItemId();
		if (id == R.id.action_settings) {
			return true;
		}
		return super.onOptionsItemSelected(item);
	}
}
