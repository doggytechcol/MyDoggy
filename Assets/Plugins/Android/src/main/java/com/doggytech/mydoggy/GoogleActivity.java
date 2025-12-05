package com.doggytech.mydoggy;

import android.content.Intent;
import android.os.Bundle;
import com.unity3d.player.UnityPlayerActivity;

public class GoogleActivity extends UnityPlayerActivity {

    private GoogleLoginController googleLoginController;

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        
        // Inicializa el controlador de Google con la actividad de Unity
        googleLoginController = new GoogleLoginController(this);
    }

    /**
     * Captura el resultado de la actividad (Intent) y lo pasa al controlador.
     */
    @Override
    protected void onActivityResult(int requestCode, int resultCode, Intent data) {
        super.onActivityResult(requestCode, resultCode, data);
        
        // Asegúrate de pasar el resultado al controlador de Google
        if (googleLoginController != null) {
            googleLoginController.onActivityResult(requestCode, resultCode, data);
        }
    }

    /**
     * Método para que Unity C# acceda al controlador de Java y lo inicie.
     */
    public GoogleLoginController getGoogleLoginController() {
        return googleLoginController;
    }
}