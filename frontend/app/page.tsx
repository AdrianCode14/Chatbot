"use client";
import { get } from "http";
import { list } from "postcss";
import React, { useEffect } from "react";
import { useState } from "react";

export default function Home() {
  const [chatContent, setChatContent] = useState<string>(
    "Bonjour, en quoi puis-je vous aider ?"
  );

  const [filesContent, setFilesContent] = useState("");

  const addToChat = (newContent: String) => {
    setChatContent(chatContent + "|||" + (newContent || " "));
  };
  const addToFiles = (newContent: String) => {
    setFilesContent("" + newContent);
  };

  async function uploadFile(event: React.ChangeEvent<HTMLInputElement>) {
    const fileInput = event.target;
    const file: File | null = fileInput.files ? fileInput.files[0] : null;
    if (!file) {
      console.log("No file selected");
      return;
    }
    const formData = new FormData();
    formData.append("file", file);

    const response = await fetch("/api/storage/upload", {
      method: "POST",
      body: formData,
    });
    if (!response.ok) {
      console.error("Failed to upload file");
      return;
    }
    console.log("File uploaded successfully");
    getFiles();
    // Gérer la mise à jour de l'affichage des fichiers si nécessaire
  }

  async function callApi(event: any) {
    if (event.key == "Enter" && event.target.value.length > 0) {
      const userInput = event.target.value;
      setChatContent((prevChatContent) => prevChatContent + "|||" + userInput);
      event.target.value = "";

      var requestBody = JSON.stringify({
        chatContent: chatContent,
        message: userInput,
      });
      const response = await fetch("/api/chat", {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
        },
        body: requestBody,
      });
      if (!response.ok) {
        console.error("Failed to send message to backend");
        return;
      }
      const data = await response.text();
      setChatContent(
        (prevChatContent) =>
          prevChatContent + "|||" + data ?? "I am here to help you"
      );
    }
  }

  async function getFiles() {
    const response = await fetch("/api/storage", {
      method: "GET",
      headers: {
        "Content-Type": "application/json",
      },
    });
    if (!response.ok) {
      console.error("Failed to send message to backend");
      return;
    }
    const data = await response.json(); // Parse the response body
    var string = "";
    for (var i = 0; i < data.length; i++) string += "," + data[i].name;

    addToFiles(string);
  }

  async function deleteFile(name: string) {
    await fetch("/api/storage", {
      method: "DELETE",
      headers: {
        "Content-Type": "application/json",
      },
      body: JSON.stringify({ message: name }),
    });
    getFiles();
  }

  //Permet de scroll automatiquement quand on envoie un nouveau message ou que gpt nous répond
  useEffect(() => {
    const lastMessage = document.querySelector(
      ".scrollable-content p:last-child"
    );
    if (lastMessage) {
      lastMessage.scrollIntoView({ behavior: "smooth" });
    }
  }, [chatContent]);

  useEffect(() => {
    getFiles();
  }, []);

  return (
    <div className="flex min-h-screen flex-row">
      {/* Partie gauche*/}
      <div className="flex-1 flex flex-col p-4 bg-gray-100 space-y-4 overflow-y-auto">
        <h1 className="text-lg font-semibold border-b border-gray-300">
          Contenu du chat
        </h1>
        <div className="scrollable-content space-y-4 flex-1 overflow-y-auto">
          {chatContent?.split("|||").map((line, index) => (
            <p
              key={index}
              className={`break-words border border-gray-300 rounded-lg p-4 w-4/5 flex items-center ${
                index % 2 === 0 ? "bg-white" : "bg-blue-500 text-white"
              } ${index % 2 === 0 ? "mr-auto" : "ml-auto"}`}
            >
              {line}
            </p>
          ))}
        </div>
        <div className="mt-auto mb-2">
          <textarea
            className="w-full h-20 p-2 border-gray-300 rounded-xl"
            placeholder="Écris quelque chose..."
            onKeyUp={callApi}
          />
        </div>
      </div>
      {/*Partie droite*/}
      <div className="flex-1 flex flex-col p-4 bg-gray-200">
        {/* Liste des fichiers*/}
        <div>
          <h2 className="text-lg font-semibold mb-2 border-b border-gray-300">
            Fichiers Uploadés
          </h2>
          {/* Ajouter la liste des fichiers ici plus tard */}
        </div>
        <div className="flex-1 scrollable-content">
          {filesContent
            .split(",")
            .filter((line) => line.trim() !== "")
            .map((file, index) => (
              <div className="flex items-center justify-between ml-auto">
                <p
                  key={index}
                  className={`overflow-hidden shadow-md shadow-gray-400 border border-gray-300 bg-white rounded-lg p-4 w-4/5 mt-2 flex items-center mx-auto justify-between`}
                >
                  <div className={"overflow-hidden"}>{file}</div>
                  <div
                    className="cursor-pointer"
                    onClick={() => deleteFile(file)}
                  >
                    <svg
                      className="h-5 w-5 fill-current text-red-500 ml-16"
                      xmlns="http://www.w3.org/2000/svg"
                      viewBox="0 0 448 512"
                    >
                      <path d="M135.2 17.7L128 32H32C14.3 32 0 46.3 0 64S14.3 96 32 96H416c17.7 0 32-14.3 32-32s-14.3-32-32-32H320l-7.2-14.3C307.4 6.8 296.3 0 284.2 0H163.8c-12.1 0-23.2 6.8-28.6 17.7zM416 128H32L53.2 467c1.6 25.3 22.6 45 47.9 45H346.9c25.3 0 46.3-19.7 47.9-45L416 128z" />
                    </svg>
                  </div>
                </p>
              </div>
            ))}
        </div>
        <div className="text-center mt-auto mb-8">
          <label
            htmlFor="fileInput"
            className="upload bg-blue-500 text-white py-3 px-6 rounded-xl cursor-pointer shadow-md shadow-gray-400"
          >
            Upload files
          </label>
          <input
            type="file"
            id="fileInput"
            className="hidden"
            onChange={uploadFile}
            multiple
          />
        </div>
      </div>
    </div>
  );
}
