import logo from './logo.svg';
import './App.css';
import FileUpload from './file-upload';

function App() {
  return (
    <div className="App">
      <h1>Upload HTML File to S3</h1>
      <FileUpload></FileUpload>
    </div>
  );
}

export default App;
