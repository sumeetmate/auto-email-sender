import React, { useState } from "react";
import AWS from 'aws-sdk';

const FileUpload = () => {
    const [file, setFile] = useState(null);

    const handleFileChange = (event) => {
        setFile(event.target.files[0]);
    };

    const uploadFile = () => {
        if(!file) return alert("Please select the file.");

        AWS.config.update({
            accessKeyId: "",
            secretAccessKey: "",
            region: "us-east-2",
        });

        const dynamoDB = new AWS.DynamoDB.DocumentClient();
        const s3 = new AWS.S3();

        const items = [
            {
                PutRequest: {
                    Item: {
                        Email: "sumeetdev1907@gmail.com",
                        TemplateName: "document.html",
                        Name: "Joe Dale"
                    }
                }
            },
            {
                PutRequest: {
                    Item: {
                        Email: "jobs.sumeetmate@gmail.com",
                        TemplateName: "document.html",
                        Name: "Steve Smith"
                    }
                }
            }
        ];

        const db_params = {
            RequestItems: {
                "UserData": items
            }
        };

        const s3_params =  {
            Bucket: "input-email-template",
            Key: file.name,
            Body: file,
            ContentType: file.type
        };

        const uploadOperation = async () => {

            try {
                const db_result = await dynamoDB.batchWrite(db_params).promise();
                console.log("DynamoDB Batch write successful:", db_result);   

                if (db_result.UnprocessedItems && Object.keys(db_result.UnprocessedItems).length === 0) {
                    const s3_result = await s3.upload(s3_params).promise();
                    console.log("File uploaded successfully:", s3_result);
                } else {
                    console.error("Some items were not processed in DynamoDB:", db_result.UnprocessedItems);
                }
            } catch (error) {
                console.error("Error updating DynamoDB or uploading to S3:", error);
            }
        };

        uploadOperation();
    };

    return (
        <div>
            <input type="file" accept=".html" onChange={handleFileChange}></input>
            <button onClick={uploadFile}>Upload Email template</button>
        </div>
    );
};

export default FileUpload;