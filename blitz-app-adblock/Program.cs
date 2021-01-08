﻿using System;
using System.IO;
using System.Net;
using System.Text;
using asardotnet;
using blitz_app_adblock.Properties;

namespace blitz_app_adblock {
    class Program {
        private static string appPath =>
            $"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}\\Programs\\Blitz\\resources";
        static void Main(string[] args) {
            ServicePointManager.Expect100Continue = true;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            bool noupdate = true;
            bool autoguest = false;
            Console.WriteLine("Bienvenido a BlitzAdBlock V.0.3b");
            foreach (string arg in args) {
                if(arg.ToLower() == "-noupdate") {
                    Console.WriteLine("Deshabilitando Blitz auto update...");
                    noupdate = true;
                }
                if (arg.ToLower() == "-autoguest") {
                    Console.WriteLine("Enabling auto sign in as guest...");
                    autoguest = true;
                }
            }

            if (File.Exists($"{appPath}\\app.asar")) {
                Console.WriteLine("Blitz encontrado!");

                try {
                    Console.WriteLine("Extrayendo archivos de configuracion...");
                    CopyFolder($"{appPath}\\app.asar.unpacked\\", $"{appPath}\\app\\");
                    var asar = new AsarArchive($"{appPath}\\app.asar");
                    var extractor = new AsarExtractor();
                    extractor.ExtractAll(asar, $"{appPath}\\app\\", true);
                } catch (IOException) {
                    Console.WriteLine("¡Error al extraer archivos! Asegúrese de que la aplicación Blitz esté cerrada antes de volver a intentarlo...");
                    Console.ReadKey();
                    return;
                }
            
                Console.WriteLine("Descargando filtros para anuncios...");
                new WebClient().DownloadFile("https://easylist.to/easylist/easylist.txt", $"{appPath}\\app\\src\\easylist.txt");
                new WebClient().DownloadFile("https://easylist.to/easylist/easyprivacy.txt", $"{appPath}\\app\\src\\easyprivacy.txt");
                new WebClient().DownloadFile("https://raw.githubusercontent.com/uBlockOrigin/uAssets/master/filters/filters.txt", $"{appPath}\\app\\src\\ublock-ads.txt");
                new WebClient().DownloadFile("https://raw.githubusercontent.com/uBlockOrigin/uAssets/master/filters/privacy.txt", $"{appPath}\\app\\src\\ublock-privacy.txt");
                new WebClient().DownloadFile("https://pgl.yoyo.org/adservers/serverlist.php?hostformat=adblock&showintro=1&mimetype=plaintext", $"{appPath}\\app\\src\\peter-lowe-list.txt");

                Console.WriteLine("Parchando...");
                string fileToPatch = $"{appPath}\\app\\src\\createWindow.js";

                // copy adblocker lib to src
                File.WriteAllBytes($"{appPath}\\app\\src\\adblocker.umd.min.js", Encoding.UTF8.GetBytes(Resources.adblocker_umd_min));

                // start writing our payload to createWindow.js
                ModifyFileAtLine("session: true,", fileToPatch, 106);
                ModifyFileAtLine(

                "try {" +
                    "const fs = require('fs');" +
                    "const { FiltersEngine, Request} = require('./adblocker.umd.min.js');" +
                    "const filters = " +
                    "fs.readFileSync(require.resolve('./easylist.txt'), 'utf-8') + '\\n' + " +
                    "fs.readFileSync(require.resolve('./easyprivacy.txt'), 'utf-8') + '\\n' + " +
                    "fs.readFileSync(require.resolve('./ublock-ads.txt'), 'utf-8') + '\\n' + " +
                    "fs.readFileSync(require.resolve('./ublock-privacy.txt'), 'utf-8') + '\\n' + " +
                    "fs.readFileSync(require.resolve('./peter-lowe-list.txt'), 'utf-8') + '\\ngoogleoptimize.com\\n';" + 
                    "const engine = FiltersEngine.parse(filters);" +

                    "windowInstance.webContents.session.webRequest.onBeforeRequest({ urls:['*://*/*']}, (details, callback) => {" +
                        "const { match } = engine.match(Request.fromRawDetails({ url: details.url}));" +
                        "if (match == true) {" +
                            "log.info('BLOCKED:', details.url);" +
                            "callback({cancel: true});" +
                        "} else {" +
                            "callback({cancel: false});" +
                        "}" +
                    "});" +
                "} catch (error) {" +
                    "log.error(error);" +
                "}"

                , fileToPatch, 119);

                // optional features
                if (noupdate)
                {
                    Console.WriteLine("Deshabilitando Blitz auto update...");
                    ModifyFileAtLine("if (false) {", $"{appPath}\\app\\src\\index.js", 267);
                    Console.WriteLine("Busca dentro de opciones en blitz, para actualizar.");

                }
                if (autoguest) { 
                    ModifyFileAtLine(

                        "autoGuest();" +
                        "function autoGuest() {" +
                            "var buttons = document.getElementsByTagName('button');" +
                            "for (var i = 0; i < buttons.length; i++) {" +
                                "if (buttons[i].getAttribute('label') == 'Login As Guest') {" +
                                    "buttons[i].click();" +
                                    "return;" +
                                "}" +
                            "}" +
                            "setTimeout(autoGuest, 1000);" +
                        "}"

                    , $"{appPath}\\app\\src\\preload.js", 18); 
                }
                System.Threading.Thread.Sleep(1000);
                Console.Write("Un...");
                System.Threading.Thread.Sleep(1000);
                Console.Write("Dos...");
                System.Threading.Thread.Sleep(1000);
                Console.Write("Tres...");
                System.Threading.Thread.Sleep(1000);
                Console.Write("CUATRO!");
                System.Threading.Thread.Sleep(1000);
                Console.WriteLine();
                Console.WriteLine();
                Console.WriteLine();
                Console.WriteLine("..::SUBLIME::.. Parche completo! <4");
                Console.WriteLine("MrBargz en lol para skins de agradecimientos");
                Console.WriteLine("Repositorio lulzsun: https://github.com/lulzsun/blitz-app-adblock");
                Console.WriteLine("Repositorio MrBargz: https://github.com/MrBargz/BlitzAdBlock");
                Console.WriteLine("Presiona cualquier tecla para salir");
            } else {
                Console.WriteLine("Blitz no esta instalado :(!");
            }
            Console.ReadKey();
        }

        static void ModifyFileAtLine(string newText, string fileName, int line_to_edit) {
            string[] arrLine = File.ReadAllLines(fileName);
            arrLine[line_to_edit - 1] = newText;
            File.WriteAllLines(fileName, arrLine);
            //Console.WriteLine(fileName + ">>> Writing to line " + line_to_edit + ": " + newText);
        }

        static public void CopyFolder(string sourceFolder, string destFolder) {
            if (!Directory.Exists(destFolder))
                Directory.CreateDirectory(destFolder);
            string[] files = Directory.GetFiles(sourceFolder);
            foreach (string file in files) {
                string name = Path.GetFileName(file);
                string dest = Path.Combine(destFolder, name);
                File.Copy(file, dest, true);
            }
            string[] folders = Directory.GetDirectories(sourceFolder);
            foreach (string folder in folders) {
                string name = Path.GetFileName(folder);
                string dest = Path.Combine(destFolder, name);
                CopyFolder(folder, dest);
            }
        }
    }
}